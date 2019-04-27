using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchChatVideo
{
    class Colors
    {

        internal struct HSL
        {
            public double H;
            public double S;
            public double L;

            public static HSL FromRGB(double r, double g, double b)
            {
                r /= 255;
                g /= 255;
                b /= 255;

                var max = Math.Max(Math.Max(r, g), b);
                var min = Math.Min(Math.Min(r, g), b);
                var d = max - min;
                var l = (max + min) / 2;

                if (max == min)
                {
                    return new HSL { H = d, S = d, L = l };
                }

                var s = l >= 0.5 ? d / (2 - max - min) : d / (max + min);
                var h = max == r ? (g - b) / d + (g < b ? 6 : 0)
                        : max == g ? (b - r) / d + 2
                        : max == b ? (r - g) / d + 4
                        : 6;

                return new HSL{ H = h * 60, S = s, L = l };
            }

            private static double HueToRGB(double q1, double q2, double hue)
            {
                if(hue > 360)
                {
                    hue -= 360;
                }
                else if(hue < 0)
                {
                    hue += 360;
                }

                if(hue < 60) { return q1 + (q2 - q1) * (hue / 60); }
                else if(hue < 180) { return q2; }
                else if(hue < 240) { return q1 + (q2 - q1) * ((240 - hue) / 60); }
                else { return q1; }
            }

            public static Color ToRGB(HSL hsl)
            {
                if(hsl.S == 0)
                {
                    var rgb = (int) (255 * hsl.L);
                    return Color.FromArgb(rgb, rgb, rgb);
                }

                var q2 = hsl.L <= .5 ? hsl.L * (1 + hsl.S) : hsl.L + hsl.S - hsl.L * hsl.S;
                var q1 = 2 * hsl.L - q2;

                int r = (int) (255 * HueToRGB(q1, q2, hsl.H + 120));
                int g = (int) (255 * HueToRGB(q1, q2, hsl.H));
                int b = (int) (255 * HueToRGB(q1, q2, hsl.H - 120));

                return Color.FromArgb(r, g, b);
            }
        }

        private static Dictionary<string, Color> cache = new Dictionary<string, Color>();

        public static void ClearCache()
        {
            cache.Clear();
        }

        private static bool IsDark(Color color)
        {
            return ((color.R * 299) + (color.G * 587) + (color.B * 114)) / 1000 < 128;
        }

        public static Color GetCorrected(Color color, Color background, string name)
        {
            if(cache.ContainsKey(name))
            {
                return cache[name];
            }

            if (color == default(Color))
            {
                var random = new Random();
                color = Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
            }

            var darkMode = IsDark(background);

            var hsl = HSL.FromRGB(color.R, color.G, color.B);

            if(darkMode)
            {
                hsl.L = Math.Max(hsl.L, .5);
                if(hsl.L < .6 && hsl.H > 196 && hsl.H < 300)
                {
                    hsl.L += Math.Sin((hsl.H - 196) / (300 - 196) * Math.PI) * hsl.S * .4;
                }

                if (hsl.L < 0.8f && (hsl.H < 22 || hsl.H > 331))
                {
                    hsl.L += (hsl.S * .1);
                }
            }
            else
            {
                hsl.S = Math.Min(.4, hsl.S);
                hsl.L = Math.Min(.5, hsl.L);
            }

            hsl.L = Math.Min(.95, hsl.L);

            return cache[name] = HSL.ToRGB(hsl);
        }
    }
}
