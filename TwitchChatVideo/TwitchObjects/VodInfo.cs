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
