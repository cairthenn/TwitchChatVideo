using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using static TwitchChatVideo.TwitchCheer;

namespace TwitchChatVideo
{
    public class Bits
    {
        public const String BaseDir = "./cheers/";
        private Dictionary<String, Dictionary<int, CheerTier>> cheer_dictionary;
        private Dictionary<string, Image> image_cache;

        public struct Cheer
        {
            public Image Emote { get; set; }
            public Color Color { get; set; }
            public int Amount { get; set; }
        }

        public Bits(List<TwitchCheer> cheers)
        {
            cheer_dictionary = new Dictionary<string, Dictionary<int, CheerTier>>();
            image_cache = new Dictionary<string, Image>();

            cheers?.ForEach(c =>
            {
                var tiers = new Dictionary<int, CheerTier>();
                c.CheerTiers.ForEach(ct =>
                {
                    if(ct.CanCheer)
                    {
                        tiers.Add(ct.MinBits, ct);
                    }
                });
                if(tiers.Count > 0)
                {
                    cheer_dictionary.Add(c.Prefix.ToLower(), tiers);
                }
            });
            
        }

        public Cheer GetCheer(string word)
        {
            var match = Regex.Match(word, @"([A-Za-z]+)(\d+)");

            if(!match.Success)
            {
                return default(Cheer);
            }

            var name = match.Groups[1].Value.ToLower();

            if (!cheer_dictionary.ContainsKey(name))
            {
                return default(Cheer);
            }

            var cheer_tiers = cheer_dictionary[name];
            var amount = int.Parse(match.Groups[2].Value);

            var max_cheer = 0;

            foreach(var key in cheer_tiers.Keys)
            {
                if(amount >= key)
                {
                    max_cheer = Math.Max(max_cheer, key);
                }
            }

            var cache_name = name + '-' + max_cheer;
            var cheer = cheer_tiers[max_cheer];

            if (image_cache.ContainsKey(cache_name))
            {
                return new Cheer()
                {
                    Emote = image_cache[cache_name],
                    Color = cheer.Color,
                    Amount = amount,
                };
            }

            var local_path = BaseDir + cache_name + ".gif";

            var img = TwitchDownloader.GetImage(local_path, cheer.Images.DarkMode.Animated[1]);
            image_cache.Add(cache_name, img);

            return new Cheer()
            {
                Emote = img,
                Color = cheer.Color,
                Amount = amount,
            };
        }
    }

    public struct TwitchCheer
    {
        public struct CheerTier
        {
            [JsonProperty("min_bits")]
            public int MinBits { get; set; }
            [JsonProperty("id")]
            public string ID { get; set; }
            [JsonProperty("color")]
            public Color Color { get; set; }
            [JsonProperty("can_cheer")]
            public bool CanCheer { get; set; }
            [JsonProperty("images")]
            public CheerImages Images { get; set; }

            public struct CheerImages
            {
                public struct CheerStyles
                {
                    [JsonProperty("animated")]
                    public Dictionary<float, string> Animated { get; set; }
                    [JsonProperty("static")]
                    public Dictionary<float, string> Static { get; set; }
                }

                [JsonProperty("dark")]
                public CheerStyles DarkMode { get; set; }
                [JsonProperty("light")]
                public CheerStyles LightMode { get; set; }
            }
        }

        [JsonProperty("prefix")]
        public string Prefix { get; set; }
        [JsonProperty("scales")]
        public float[] Scales { get; set; }
        [JsonProperty("tiers")]
        public List<CheerTier> CheerTiers { get; set; }
    }
}
