using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TwitchChatVideo.Properties;

namespace TwitchChatVideo
{
    class TwitchDownloader
    {
        private const string BaseURLChat = "https://api.twitch.tv/v5/videos/{0}/comments?cursor={1}";
        private const string BaseURLVideo = "https://api.twitch.tv/v5/videos/{0}";
        private const string BaseURLBits = "https://api.twitch.tv/v5/bits/actions?channel_id={0}";
        private const string BaseURLGlobalBadges = "https://badges.twitch.tv/v1/badges/global/display";
        private const string BaseURLChannelBadges = "https://badges.twitch.tv/v1/badges/channels/{0}/display";


        public static Image GetImage(string local_path, string url)
        {
            var image = DownloadImageFromURL(url);

            image?.Save(local_path);

            return image;
        }

        public static Image DownloadImageFromURL(string url)
        {

            try
            {
                var request = WebRequest.Create(url);
                request.Timeout = 10000;
                var response = request.GetResponse();
                var stream = response.GetResponseStream();
                var image = Image.FromStream(stream);
                response.Close();
                return image;
            }
            catch (WebException ex)
            {
                return null;
            }
        }

        public static async Task<VodInfo> DownloadVideoInfoAsync(string id, IProgress<VideoProgress> progress = null, CancellationToken ct = default(CancellationToken))
        {
            progress?.Report(new VideoProgress(0, 1, VideoProgress.VideoStatus.Info));
            var url = string.Format(BaseURLVideo, id);
            var result = (await DownloadAsync(url, progress, ct))?.ToObject<VodInfo>();
            progress?.Report(new VideoProgress(1, 1, VideoProgress.VideoStatus.Info));
            return result;
        }

        public static async Task<Bits> DownloadBitsAsync(string id, IProgress<VideoProgress> progress = null, CancellationToken ct = default(CancellationToken))
        {
            progress?.Report(new VideoProgress(0, 1, VideoProgress.VideoStatus.Cheers));
            var url = string.Format(BaseURLBits, id);
            var result = (await DownloadAsync(url, progress, ct))["actions"]?.ToObject<List<TwitchCheer>>();
            progress?.Report(new VideoProgress(1, 1, VideoProgress.VideoStatus.Cheers));
            return new Bits(result);
        }

        public static async Task<Badges> DownloadBadgesAsync(string id, IProgress<VideoProgress> progress = null, CancellationToken ct = default(CancellationToken))
        {
            progress?.Report(new VideoProgress(0, 1, VideoProgress.VideoStatus.Badges));
            var results = new Dictionary<string, Dictionary<string, TwitchBadge>>();

            var global_results = await DownloadAsync(BaseURLGlobalBadges, progress, ct);

            if(global_results?["badge_sets"] != null)
            {
                foreach (JProperty set in global_results?["badge_sets"])
                {
                    results.Add(set.Name, set.Value["versions"].ToObject<Dictionary<String, TwitchBadge>>());
                }
            }

            var channel_results = await DownloadAsync(string.Format(BaseURLChannelBadges, id), progress, ct);

            if (channel_results?["badge_sets"]?["bits"] != null)
            {
                foreach (JProperty badge in channel_results["badge_sets"]["bits"]["versions"])
                {
                    results["bits"][badge.Name] = badge.Value.ToObject<TwitchBadge>();
                }
            }

            if (channel_results?["badge_sets"]?["subscriber"] != null)
            {
                foreach (JProperty badge in channel_results["badge_sets"]["subscriber"]["versions"])
                {
                    results["subscriber"][badge.Name] = badge.Value.ToObject<TwitchBadge>();
                }
            }
            progress?.Report(new VideoProgress(1, 1, VideoProgress.VideoStatus.Badges));
            return new Badges(id, results);
        }

        public static async Task<List<ChatMessage>> GetChatAsync(string id, double duration, IProgress<VideoProgress> progress = null, CancellationToken ct = default(CancellationToken))
        {
            return await Task.Run(async () =>
            {
                try {
                    var file = string.Format("./{0}/{1}.json", ChatVideo.LogDirectory, id);
                    if (File.Exists(file))
                    {
                        using (var f = File.OpenText(file))
                        using (var r = new JsonTextReader(f))
                        {
                            return JToken.ReadFrom(r).ToObject<List<ChatMessage>>();
                        }
                    }

                    var chat_history = await DownloadChatHistoryAsync(id, duration, progress, ct);

                    if (chat_history != null)
                    {
                        using (var f = File.CreateText(file))
                        using (var w = new JsonTextWriter(f))
                        {
                            chat_history?.WriteTo(w);
                        }
                    }

                    return chat_history?.ToObject<List<ChatMessage>>();
                }
                finally
                {
                    progress?.Report(new VideoProgress(1, 1, VideoProgress.VideoStatus.Chat));
                }
            });
        }

        private static async Task<JObject> DownloadAsync(string url, IProgress<VideoProgress> progress = null, CancellationToken ct = default(CancellationToken))
        {
            if(ct.IsCancellationRequested)
            {
                return null;
            }

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Headers.Add("Client-ID", Resources.client_id);
            req.Accept = "application/vnd.twitchtv.v5+json";

            try
            {
                using (var stream = new StreamReader((await req.GetResponseAsync())?.GetResponseStream()))
                {
                    return JObject.Parse(stream.ReadToEnd());
                }
            }
            catch (WebException e)
            {
                System.Windows.MessageBox.Show(string.Format("Unable to reach {0}: \n\n{1}", url, e.Message));
                return null;
            }
        }

        private static async Task<JObject> DownloadChatSegmentAsync(string id, string cursor = "", IProgress<VideoProgress> progress = null, CancellationToken ct = default(CancellationToken))
        {
            return await DownloadAsync(string.Format(BaseURLChat, id, cursor, progress, ct));
        }

        private static async Task<JToken> DownloadChatHistoryAsync(string id, double duration, IProgress<VideoProgress> progress, CancellationToken ct)
        {
            var chat = new JObject();

            var segment = await DownloadChatSegmentAsync(id, null, progress, ct);

            if(!segment.HasValues)
            {
                return null;
            }

            chat.Add("comments", segment["comments"]);

            while (segment["_next"] != null)
            {
                segment = await DownloadChatSegmentAsync(id, segment["_next"].ToString(), progress, ct);

                if (ct.IsCancellationRequested || segment == null)
                {
                    return null;
                }

                chat.Merge(segment, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                progress?.Report(new VideoProgress((long)chat["comments"].Last?["content_offset_seconds"], (long)duration, VideoProgress.VideoStatus.Chat));
            }

            return chat["comments"];
        }
    }
}

/*
    Twitch Chat Video

    Copyright (C) 2019 Cair

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
