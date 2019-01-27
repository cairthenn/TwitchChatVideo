using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TwitchChatVideo.Properties;

namespace TwitchChatVideo
{
    public class FFZ
    {
        public const String BaseDir = "./emotes/ffz/";
        private const String EmoteSize = "1";
        private const String GlobalURL = "https://api.frankerfacez.com/v1/set/global";
        private const String BaseURL = "https://api.frankerfacez.com/v1/room/";
        private const String EmoteDownload = "https://cdn.frankerfacez.com/emoticon/{0}/{1}";

        private Dictionary<String, FFZEmote> emote_dictionary;
        private Dictionary<String, Image> image_cache;

        private FFZ(Dictionary<string, FFZEmote> emotes)
        {
            emote_dictionary = emotes;
            image_cache = new Dictionary<string, Image>();
        }

        public static async Task<FFZ> CreateAsync(string channel, IProgress<VideoProgress> progress, System.Threading.CancellationToken ct)
        {
            progress?.Report(new VideoProgress(0, 1, VideoProgress.VideoStatus.FFZ));
            var global_req = (HttpWebRequest)WebRequest.Create(GlobalURL);
            var channel_req = (HttpWebRequest)WebRequest.Create(BaseURL + channel);
            return await Task.Run(async () =>
            {
                try
                {
                    using (var global_stream = new StreamReader((await global_req.GetResponseAsync())?.GetResponseStream()))
                    using (var channel_stream = new StreamReader((await channel_req.GetResponseAsync())?.GetResponseStream()))
                    {
                        progress?.Report(new VideoProgress(1, 1, VideoProgress.VideoStatus.FFZ));
                        var global_emotes = JObject.Parse(global_stream.ReadToEnd()).ToObject<FFZGlobal>();
                        var channel_emotes = JObject.Parse(channel_stream.ReadToEnd()).ToObject<FFZRoom>();

                        var emote_dictionary = channel_emotes.EmoteSets.First().Value.Emotes.ToDictionary(x => x.Name);

                        foreach (var set in global_emotes.Sets)
                        {
                            global_emotes.EmoteSets[set].Emotes.ForEach(x => emote_dictionary.Add(x.Name, x));
                        }

                        return new FFZ(emote_dictionary);
                    }
                }
                catch (WebException e)
                {
                    System.Windows.MessageBox.Show(string.Format("Unable to download FFZ emotes: \n\n{0}", e.Message));
                    return new FFZ(new Dictionary<string, FFZEmote>());
                }
            });
        }

        public Image GetEmote(string name)
        {
            if (!emote_dictionary.ContainsKey(name))
            {
                return null;
            }

            if (image_cache.ContainsKey(name))
            {
                return image_cache[name];
            }

            var emote = emote_dictionary[name];
            var local_path = BaseDir + emote.ID + ".png";
            var url = string.Format(EmoteDownload, emote.ID, EmoteSize);
            var img = TwitchDownloader.GetImage(local_path, url);

            image_cache.Add(name, img);

            return img;
        }

        private FFZ() { }

        public static FFZ SampleFFZ = new FFZ()
        {
            emote_dictionary = new Dictionary<string, FFZEmote>()
            {
                { "D:", null },
                { "Pog", null },
                { "PepeHands", null },
            },

            image_cache = new Dictionary<string, Image>()
            {
                { "D:", Resources.d_colon },
                { "Pog", Resources.pog },
                { "PepeHands", Resources.pepe_hands },
            }
        };


        public struct FFZGlobal
        {
            [JsonProperty("default_sets")]
            public int[] Sets { get; set; }
            [JsonProperty("sets")]
            public Dictionary<int, FFZSet> EmoteSets { get; set; }
        }

        public struct FFZRoom
        {
            public class FFZUser
            {
                [JsonProperty("id")]
                public string ID { get; set; }
                [JsonProperty("display_name")]
                public string Name { get; set; }
                [JsonProperty("is_group")]
                public bool IsGroup { get; set; }
            }

            [JsonProperty("room")]
            public FFZUser User { get; set; }
            [JsonProperty("sets")]
            public Dictionary<int, FFZSet> EmoteSets { get; set; }
        }

        public class FFZEmote
        {
            [JsonProperty("height")]
            public int Height { get; set; }
            [JsonProperty("width")]
            public int Width { get; set; }
            [JsonProperty("id")]
            public string ID { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class FFZSet
        {
            [JsonProperty("_type")]
            public int Type { get; set; }
            [JsonProperty("emoticons")]
            public List<FFZEmote> Emotes { get; set; }
        }
    }
}
