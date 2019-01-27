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

            if(r_dif < 128 && g_dif < 128 && b_dif < 128)
            {
                return Color.FromArgb((value.R + 128) % 255, (value.G + 128) % 255, (value.B + 128) % 255);
            }

            return value;
        }
    }
}
