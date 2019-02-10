using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TwitchChatVideo.Properties;

namespace TwitchChatVideo
{
    public class ChatHandler : IDisposable
    {
        private static Image text_sizes = new Bitmap(1, 1);
        private static Graphics g = Graphics.FromImage(text_sizes);

        private static Dictionary<string, Color> default_colors = new Dictionary<string, Color>();

        public Font Font { get; }
        public Font BoldFont { get; }
        public Color ChatColor { get; }
        public Color BGColor { get; }
        public float Spacing { get; }
        public bool ShowBadges { get; }
        public bool VodChat { get; }
        public float Width { get; }
        public BTTV BTTV { get; }
        public FFZ FFZ { get; }
        public Badges Badges { get; }
        public Bits Bits { get; }

        public class Line
        {

            public float OffsetX { get; }
            public float OffsetY { get; }
            public List<Drawable> Drawables { get; }

            public float Height => Drawables.Max(x => x.Height);

            public Line(float x, float y, List<Drawable> dl)
            {
                OffsetX = x;
                OffsetY = y;
                Drawables = dl;
            }
        }

        public abstract class Drawable : IDisposable
        {
            public float OffsetX { get; set; }
            public float OffsetY { get; set; }
            public abstract float Height { get; }

            public abstract void Dispose();
        }

        public class Emote : Drawable
        {
            public Image Image { get; }
            public override float Height => Image.Height;

            private int max_frames;
            private float frame_delay;
            private System.Drawing.Imaging.FrameDimension dimension;
            private const int FRAME_DELAY_ID = 0x5100;

            public Emote(float offset_x, float offset_y, Image image)
            {
                OffsetX = offset_x;
                OffsetY = offset_y;
                Image = image;

                dimension = new System.Drawing.Imaging.FrameDimension(image.FrameDimensionsList[0]);
                max_frames = image.GetFrameCount(dimension);
                if (max_frames > 1)
                {
                    var fd_bytes = image.GetPropertyItem(FRAME_DELAY_ID);
                    frame_delay = fd_bytes != null ? BitConverter.ToInt32(fd_bytes.Value, 0) : 0;
                }
                else
                {
                    frame_delay = 0;
                }
            }

            public void SetFrame(int f)
            {
                if (max_frames > 1)
                {
                    Image.SelectActiveFrame(dimension, f % max_frames);
                }
            }

            public override void Dispose()
            {
            }
        }

        public class Badge : Drawable
        {
            public Image Image { get; }
            public override float Height => Image.Height;

            public Badge(float offset_x, float offset_y, Image image)
            {
                OffsetX = offset_x;
                OffsetY = offset_y;
                Image = image;
            }

            public override void Dispose()
            {
            }
        }

        public class User : Drawable
        {
            public override float Height { get; }
            public string Name { get; }
            public Brush Brush { get; }
            public Font Font { get; }

            public User(float offset_x, float offset_y, float height, string name, Color color, Font f)
            {
                Height = height;
                OffsetX = offset_x;
                OffsetY = offset_y;
                Name = name;
                Font = (Font) f.Clone();
                Brush = new SolidBrush(color);
            }

            public override void Dispose()
            {
                Brush?.Dispose();
                Font?.Dispose();
            }
        }

        public class Text : Drawable
        {
            public override float Height { get; }
            public string Message { get; }
            public Brush Brush { get; }
            public Font Font { get; }

            public Text(float offset_x, float offset_y, float height, string text, Color color, Font f)
            {
                OffsetX = offset_x;
                OffsetY = offset_y;
                Message = text;
                Font = (Font) f.Clone();
                Height = height;
                Brush = new SolidBrush(color);
            }

            public override void Dispose()
            {
                Brush?.Dispose();
                Font?.Dispose();
            }
        }

        public static SizeF MeasureText(string text, Font font)
        {
            lock (g) {
                return g.MeasureString(text, font);
            }
        }

        public ChatHandler(ChatVideo cv, BTTV bttv, FFZ ffz, Badges badges, Bits bits)
        {
            Font = (Font) cv.Font.Clone();
            BoldFont = new Font(Font, FontStyle.Bold);
            ChatColor = Color.FromArgb(cv.ChatColor.A, cv.ChatColor.R, cv.ChatColor.G, cv.ChatColor.B);
            BGColor = Color.FromArgb(cv.BGColor.A, cv.BGColor.R, cv.BGColor.G, cv.BGColor.B);
            Width = cv.Width;
            Spacing = cv.LineSpacing;
            ShowBadges = cv.ShowBadges;
            BTTV = bttv;
            FFZ = ffz;
            Badges = badges;
            Bits = bits;
        }

        public void Dispose()
        {
            Font?.Dispose();
            BoldFont?.Dispose();
        }

        public DrawableMessage MakeDrawableMessage(ChatMessage message)
        {
            var lines = new List<Line>();

            var emoji_builder = new StringBuilder();

            // \p{Cs} or \p{Surrogate}: one half of a surrogate pair in UTF-16 encoding.
            var words = Regex.Replace(message.Text, @"\p{Cs}\p{Cs}", m =>
            {   
                message.Emotes?.Where(e => e.Begin > m.Index)?.ToList().ForEach(e2 => e2.Begin += 1);
                return Emoji.Exists(m.Value) ? ' ' + m.Value + ' ' : "?";
            }).Split(' ').Where(s => s != string.Empty);

            var builder = new StringBuilder();

            float x = ChatVideo.HorizontalPad;
            float y = 0;
            float maximum_y_offset = 0;

            int cursor = 0;

            List<Drawable> dl = new List<Drawable>();

            var user_tag = message.Name + ":";
            var user_size = MeasureText(user_tag, BoldFont);

            if (ShowBadges)
            {
                message.Badges?.ForEach(b =>
                {
                    var img = Badges?.Lookup(b.ID, b.Version);
                    if (img != null) {
                        var align_vertical = user_size.Height * .5f - img.Height * .5f;
                        if (align_vertical < 0)
                        {
                            maximum_y_offset = Math.Max(Math.Abs(align_vertical), maximum_y_offset);
                        }
                        var badge = new Badge(x, align_vertical, img);
                        dl.Add(badge);
                        x += img.Width + ChatVideo.BadgePad;
                    }
                });
            }

            var color = message.Color;

            if(color == null)
            {
                if (default_colors.ContainsKey(message.Name))
                {
                    color = default_colors[message.Name];
                }
                else
                {
                    var random = new Random();
                    color = Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
                    default_colors.Add(message.Name, color);
                }
            }

            dl.Add(new User(x, y, user_size.Height, user_tag, color.Contrast(BGColor), BoldFont));
            x += user_size.Width;

            Action new_line = delegate
            {
                y += maximum_y_offset;
                var line = new Line(ChatVideo.HorizontalPad, y, dl);
                lines.Add(line);
                y += maximum_y_offset + line.Height;
                maximum_y_offset = 0;
                dl = new List<Drawable>();
                x = ChatVideo.HorizontalPad;
            };

            Action empty_builder = delegate
            {
                var txt = builder.ToString();
                var sz = MeasureText(txt, Font);
                dl.Add(new Text(x, 0, sz.Height, txt, ChatColor, Font));
                builder.Clear();
                x += sz.Width;
            };

            foreach (var word in words)
            {
                var emote = Emoji.GetEmoji(word) ?? FFZ?.GetEmote(word) ?? BTTV?.GetEmote(word) ??
                    TwitchEmote.GetEmote(message.Emotes?.FirstOrDefault(e => e.Begin == cursor)?.ID);

                cursor += word.Length + 1;

                if(emote != null)
                {
                    if (builder.Length > 0)
                    {
                        empty_builder();
                    }

                    if (x + emote.Width + ChatVideo.HorizontalPad > Width)
                    {
                        new_line();
                    }

                    var align_vertical = Font.Height * .5f - emote.Height * .5f;
                    if (align_vertical < 0)
                    {
                        maximum_y_offset = Math.Max(Math.Abs(align_vertical), maximum_y_offset);
                    }
                    x += ChatVideo.EmotePad;
                    dl.Add(new Emote(x, align_vertical, emote));
                    x += emote.Width;

                    continue;
                }

                var cheer = message.Content.Bits > 0 ? Bits?.GetCheer(word) : default(Bits.Cheer);

                if (cheer?.Amount > 0)
                {
                    var ch = cheer.Value;

                    if (builder.Length > 0)
                    {
                        empty_builder();
                    }

                    if (x + ch.Emote.Width + ChatVideo.HorizontalPad > Width)
                    {
                        new_line();
                    }

                    var align_vertical = Font.Height * .5f - ch.Emote.Height * .5f;
                    if (align_vertical < 0)
                    {
                        maximum_y_offset = Math.Max(Math.Abs(align_vertical), maximum_y_offset);
                    }
                    x += ChatVideo.EmotePad;
                    dl.Add(new Emote(x, align_vertical, ch.Emote));
                    x += ch.Emote.Width;

                    var cheer_amount = cheer?.Amount.ToString();
                    var sz = MeasureText(cheer_amount, Font);
                    if (x + sz.Width + ChatVideo.HorizontalPad >= Width)
                    {
                        new_line();
                    }
                    dl.Add(new Text(x, 0, sz.Height, cheer_amount, ch.Color, Font));
                    x += sz.Width;
                    continue;
                }

                var text = builder.ToString();
                var word_tag = word + ' ';
                var size = MeasureText(text + word_tag, Font);
                if (x + size.Width + ChatVideo.HorizontalPad >= Width)
                {
                    empty_builder();
                    new_line();
                    builder.Append(word_tag);
                }
                else
                {
                    builder.Append(word_tag);
                }
        }

            if (builder.Length > 0)
            {
                var text = builder.ToString();
                var size = MeasureText(text, Font);
                dl.Add(new Text(x, 0, size.Height, text, ChatColor, Font));
            }

            if(dl.Count > 0)
            {
                lines.Add(new Line(ChatVideo.HorizontalPad, y + maximum_y_offset, dl));
            }

            return new DrawableMessage {
                Lines = lines,
                StartFrame = (uint) (message.TimeOffset * ChatVideo.FPS),
                Live = message.Source == "chat",
            };
        }

        public struct DrawableMessage
        {
            public List<Line> Lines { get; internal set; }
            public uint StartFrame { get; internal set; }
            public bool Live { get; internal set; }
        }

        public static Stack<DrawableMessage> MakeSampleChat(ChatVideo cv)
        {
            using (var ch = new ChatHandler(cv, null, FFZ.SampleFFZ, Badges.SampleBadges, null))
            {
                var lines = new Stack<DrawableMessage>();
                MakeSampleMessages().ForEach(m => lines.Push(ch.MakeDrawableMessage(m)));
                return lines;
            }
        }


        public static List<ChatMessage> MakeSampleMessages()
        {
            using (var f = new StreamReader(new MemoryStream(Resources.SampleChat), Encoding.Default))
            using (var r = new JsonTextReader(f))
            {
                return JToken.ReadFrom(r).ToObject<List<ChatMessage>>();
            }
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
