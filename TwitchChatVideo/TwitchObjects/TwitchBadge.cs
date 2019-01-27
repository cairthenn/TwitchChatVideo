using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using TwitchChatVideo.Properties;

namespace TwitchChatVideo
{
    public struct TwitchBadge
    {
        [JsonProperty("image_url_1x")]
        public String URL { get; set; }
        [JsonProperty("title")]
        public String Name { get; set; }
    }

    public class Badges
    {
        public const String BaseDir = "./badges/";
        private Dictionary<String, Dictionary<String, TwitchBadge>> lookup;
        private Dictionary<String, Image> image_cache;
        private String id;

        public Badges(String id, Dictionary<String, Dictionary<String, TwitchBadge>> lookup)
        {
            this.id = id;
            this.lookup = lookup;
            this.image_cache = new Dictionary<string, Image>();
            Directory.CreateDirectory(BaseDir + id);
        }

        private Badges() { }

        public static Badges SampleBadges = new Badges()
        {
            id = "",
            image_cache = new Dictionary<string, Image>()
            {
                {  "/broadcaster-1", Resources.broadcaster_1 },
                {  "/partner-1", Resources.partner_1 },
                {  "/subscriber-1", Resources.subscriber_1 },
                {  "/subscriber-3", Resources.subscriber_3 },
                {  "/subscriber-6", Resources.subscriber_6 },
                {  "/subscriber-12", Resources.subscriber_6 },
                {  "/moderator-1", Resources.moderator_1 },
                {  "/bits-1", Resources.bits_1 },
                {  "/bits-100", Resources.bits_100 },
                {  "/bits-1000", Resources.bits_1000 },
                {  "/bits-5000", Resources.bits_5000 },
                {  "/bits-10000", Resources.bits_10000 },
                {  "/bits-25000", Resources.bits_25000 },
                {  "/bits-50000", Resources.bits_50000 },
                {  "/bits-100000", Resources.bits_100000 },
                {  "/bits-charity-1", Resources.bits_charity_1 },
                {  "/bits-leader-1", Resources.bits_leader_1 },
                {  "/bits-turbo-1", Resources.turbo_1 },
                {  "/bits-premium-1", Resources.premium_1 },
                {  "/vip-1", Resources.vip_1 },
            },

        };

        public Image Lookup(String type, String version)
        {
            var concat = id + "/" + type + "-" + version;

            if (image_cache.ContainsKey(concat))
            {
                return image_cache[concat];
            }

            var badge = lookup.ContainsKey(type) && lookup[type].ContainsKey(version) ? lookup[type][version] : default(TwitchBadge);

            if (badge.Name == string.Empty)
            {
                return null;
            }

            var local_path = BaseDir + concat;
            var img = TwitchDownloader.GetImage(local_path, badge.URL);

            image_cache[concat] = img;

            return img;
        }

    }
}
