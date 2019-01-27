using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TwitchChatVideo
{
    public class BTTV
    {
        public const string BaseDir = "./emotes/bttv/";
        const string EmoteSize = "1x";
        const string GlobalURL = "https://api.betterttv.net/2/emotes/";
        const string BaseURL = "https://api.betterttv.net/2/channels/";
        const string EmoteDownload = "https://cdn.betterttv.net/emote/{0}/{1}";

        private Dictionary<string, EmoteList.Emote> emote_dictionary;
        private Dictionary<string, Image> image_cache;

        internal BTTV(Dictionary<string, EmoteList.Emote> emote_dictionary)
        {
            this.image_cache = new Dictionary<string, Image>();
            this.emote_dictionary = emote_dictionary;
        }

        public static async Task<BTTV> CreateAsync(string channel, IProgress<VideoProgress> progress, System.Threading.CancellationToken ct)
        {
            progress?.Report(new VideoProgress(0, 1, VideoProgress.VideoStatus.BTTV));
            var global_req = (HttpWebRequest)WebRequest.Create(GlobalURL);
            var channel_req = (HttpWebRequest)WebRequest.Create(BaseURL + channel);
            return await Task.Run(async () =>
            {
                try
                {
                    using (var global_stream = new StreamReader((await global_req.GetResponseAsync())?.GetResponseStream()))
                    using (var channel_stream = new StreamReader((await channel_req.GetResponseAsync())?.GetResponseStream()))
                    {
                        progress?.Report(new VideoProgress(1, 1, VideoProgress.VideoStatus.BTTV));
                        var global_emotes = JObject.Parse(global_stream.ReadToEnd()).ToObject<EmoteList>();
                        var channel_emotes = JObject.Parse(channel_stream.ReadToEnd()).ToObject<EmoteList>();
                        return new BTTV(channel_emotes.Emotes.Concat(global_emotes.Emotes).ToDictionary(x => x.Code));
                    }
                }
                catch (WebException e)
                {
                    System.Windows.MessageBox.Show(string.Format("Unable to download BTTV emotes: \n\n{0}", e.Message));
                    return new BTTV(new Dictionary<string, EmoteList.Emote>());
                }
            });
        }

        public Image GetEmote(string name)
        {
            if(!emote_dictionary.ContainsKey(name))
            {
                return null;
            }

            if (image_cache.ContainsKey(name))
            {
                return image_cache[name];
            }

            var emote = emote_dictionary[name];
            var local_path = BaseDir + emote.ID + "." + emote.ImageType;
            var url = string.Format(EmoteDownload, emote.ID, EmoteSize);
            var img = TwitchDownloader.GetImage(local_path, url);

            image_cache.Add(name, img);

            return img;
        }

        public class EmoteList
        {
            [JsonProperty("urlTemplate")]
            public string BaseURL { get; set; }
            [JsonProperty("emotes")]
            public Emote[] Emotes { get; set; }

            public class Emote
            {
                [JsonProperty("id")]
                public string ID { get; set; }
                [JsonProperty("channel")]
                public string Channel { get; set; }
                [JsonProperty("code")] 
                public string Code { get; set; }
                [JsonProperty("imageType")]
                public string ImageType { get; set; }
            }
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
