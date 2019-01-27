using System;
using System.Linq;
using System.ComponentModel;
using System.Drawing;

namespace TwitchChatVideo
{
    public static class Extensions
    {
        public static string GetDescription(this Enum value)
        {
            var attribute = value.GetType()
                .GetField(value.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .SingleOrDefault() as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }

        public static Color Contrast(this Color value, Color other)
        {
            var r_dif = (byte) (value.R - other.R);
            var g_dif = (byte) (value.G - other.G);
            var b_dif = (byte) (value.B - other.B);

            if(r_dif < 64 && g_dif < 64 && b_dif < 64)
            {
                return Color.FromArgb((value.R + 128) % 255, (value.G + 128) % 255, (value.B + 128) % 255);
            }

            return value;
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
