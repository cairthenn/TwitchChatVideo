using System.Collections.Generic;
using System.Drawing;
using TwitchChatVideo.Properties;

namespace TwitchChatVideo
{

    public class TwitchEmote
    {
        public const string BaseDir = "./emotes/twitch/";
        const string EmoteSize = "1.0";
        const string EmoteDownload = "https://static-cdn.jtvnw.net/emoticons/v1/{0}/{1}";

        private static Dictionary<string, Image> image_cache = new Dictionary<string, Image>()
        {
            { "1", Resources.smile },
            { "9", Resources.heart },
            { "clintFug", Resources.clint_fug },
            { "dprezoL", Resources.lorde },
        };

        public static Image GetEmote(string name)
        {
            if(name == null)
            {
                return null;
            }

            if(image_cache.ContainsKey(name))
            {
                return image_cache[name];
            }

            var emote_path = BaseDir + name + ".png";
            var url = string.Format(EmoteDownload, name, EmoteSize);
            var img = TwitchDownloader.GetImage(emote_path, url);

            image_cache.Add(name, img);

            return img;
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
