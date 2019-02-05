using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace TwitchChatVideo
{
    public struct ChatMessage
    {
        private static Random random = new Random();
        private Color default_color = Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
        
        public string Name => User.Name;
        public Color Color => Content?.Color ?? default_color;
        public string Text => Content.Text;
        public List<Message.Emote> Emotes => Content.Emotes;
        public List<Message.Badge> Badges => Content.Badges;


        public class Commenter
        {
            [JsonProperty("display_name")]
            public string Name { get; set; }
            [JsonProperty("name")]
            public string Username { get; set; }
            [JsonProperty("type")]
            public string Type { get; set; }
        }

        public class Message
        {
            public class Emote
            {
                [JsonProperty("_id")]
                public string ID { get; set; }
                public string URL => string.Format("https://static-cdn.jtvnw.net/emoticons/v1/{0}/1.0", ID);
                [JsonProperty("begin")]
                public int Begin { get; set; }
                [JsonProperty("end")]
                public int End { get; set; }
            }

            public class Badge
            {
                [JsonProperty("_id")]
                public string ID { get; set; }
                [JsonProperty("version")]
                public string Version { get; set; }
            }

            [JsonProperty("body")]
            public string Text { get; set; }
            [JsonProperty("is_action")]
            public bool IsAction { get; set; }
            [JsonProperty("user_badges")]
            public List<Badge> Badges { get; set; }
            [JsonProperty("emoticons")]
            public List<Emote> Emotes { get; set; }
            [JsonProperty("user_color")]
            public Color? Color { get; set; }
            [JsonProperty("bits_spent")]
            public int Bits { get; set; }
        }

        [JsonProperty("commenter")]
        public Commenter User { get; set; }
        [JsonProperty("message")]
        public Message Content { get; set; }

        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [JsonProperty("content_offset_seconds")]
        public double TimeOffset { get; set; }
        [JsonProperty("source")]
        public string Source;
        [JsonProperty("state")]
        public string State;
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
