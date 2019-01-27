using System;
using Newtonsoft.Json;

namespace TwitchChatVideo
{
    public struct VodInfo
    {
        public string Streamer => Channel.Name;
        public string StreamerID => Channel.ID;

        [JsonProperty("_id")]
        public string ID { get; set; }

        public class ChannelInfo
        {
            [JsonProperty("_id")]
            public string ID { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
        }

        [JsonProperty("channel")]
        public ChannelInfo Channel { get; set; }
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("length")]
        public double Duration { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("game")]
        public string Game { get; set; }
    }
}
