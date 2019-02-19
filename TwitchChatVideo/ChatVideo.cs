using Accord.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static TwitchChatVideo.ChatHandler;
using Text = TwitchChatVideo.ChatHandler.Text;

namespace TwitchChatVideo
{
    public class ChatVideo : IDisposable
    {
        public const string OutputDirectory = "./output/";
        public const string LogDirectory = "./logs/";
        public const int EmotePad = 3;
        public const int BadgePad = 3;
        public const int HorizontalPad = 5;
        public const int VerticalPad = 5;

        public const int FPS = 24;
        public const VideoCodec Codec = VideoCodec.H264;

        public string ID { get; set; }
        public Color BGColor { get; internal set; }
        public Color ChatColor { get; internal set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public Font Font { get; internal set; }
        public bool VodChat { get; internal set; }
        public float LineSpacing { get; internal set; }
        public bool ShowBadges { get; internal set; }

        public ChatVideo(ViewModel vm)
        {
            ID = vm.URL?.Split('/').LastOrDefault() ?? vm.URL ?? "";
            LineSpacing = vm.LineSpacing;
            BGColor = Color.FromArgb(vm.BGColor.A, vm.BGColor.R, vm.BGColor.G, vm.BGColor.B);
            ChatColor = Color.FromArgb(vm.ChatColor.A, vm.ChatColor.R, vm.ChatColor.G, vm.ChatColor.B);
            Width = (int) vm.Width;
            Height = (int)vm.Height;
            Font = new Font(vm.FontFamily.ToString(), vm.FontSize);
            VodChat = vm.VodChat;
            ShowBadges = vm.ShowBadges;
        }

        public void Dispose()
        {
            Font?.Dispose();
        }

        public async Task<bool> CreateVideoAsync(IProgress<VideoProgress> progress, CancellationToken ct)
        {
            return await Task.Run(async () =>
            {
                var video = await TwitchDownloader.DownloadVideoInfoAsync(ID, progress, ct);

                if (video?.ID == null)
                {
                    return false;
                }

                var bits = await TwitchDownloader.DownloadBitsAsync(video.StreamerID, progress, ct);
                var badges = await TwitchDownloader.DownloadBadgesAsync(video.StreamerID, progress, ct);
                var bttv = await BTTV.CreateAsync(video.Streamer, progress, ct);
                var ffz = await FFZ.CreateAsync(video.Streamer, progress, ct);
                var messages = await TwitchDownloader.GetChatAsync(ID, video.Duration, progress, ct);

                if(ct.IsCancellationRequested)
                {
                    return false;
                }

                using (var chat_handler = new ChatHandler(this, bttv, ffz, badges, bits))
                {
                    int current = 0;

                    try
                    {
                        var drawables = messages?.Select(m =>
                        {
                            progress?.Report(new VideoProgress(++current, messages.Count, VideoProgress.VideoStatus.Drawing));
                            return chat_handler.MakeDrawableMessage(m);
                        }).ToList();


                        var max = (int)(FPS * video.Duration);

                        var path = string.Format("{0}{1}-{2}.mp4", OutputDirectory, video.Streamer, video.ID);
                        var result = await WriteVideoFrames(path, drawables, 0, max, progress, ct);
                        progress?.Report(new VideoProgress(1, 1, VideoProgress.VideoStatus.CleaningUp));
                        drawables.ForEach(d => d.Lines.ForEach(l => l.Drawables.ForEach(dr => dr.Dispose())));
                    
                        return result;
                    }
                    catch (Exception e)
                    {
                        using (StreamWriter w = File.AppendText("error.txt"))
                        {
                            w.WriteLine($"{DateTime.Now.ToLongTimeString()} : {e.ToString()}");
                        }

                        return false;
                    };
                }
            });
        }

        public async Task<bool> WriteVideoFrames(string path, List<DrawableMessage> lines, int start_frame, int end_frame, IProgress<VideoProgress> progress = null, CancellationToken ct = default(CancellationToken))
        {
            var drawable_messages = new Stack<DrawableMessage>();
            var last_chat = 0;

            return await Task.Run(() =>
            {
                using (var writer = new VideoFileWriter())
                {
                    using (var bmp = new Bitmap(Width, Height))
                    {
                        writer.Open(path, Width, Height, FPS, Codec);
                        var bounds = new Rectangle(0, 0, Width, Height);

                        for (int i = start_frame; i <= end_frame; i++)
                        {
                            if (ct.IsCancellationRequested)
                            {
                                progress?.Report(new VideoProgress(0, 1, VideoProgress.VideoStatus.Idle));
                                return false;
                            }

                            progress?.Report(new VideoProgress(i, end_frame, VideoProgress.VideoStatus.Rendering));

                            if (last_chat < lines.Count)
                            {
                                // Note that this intentionally pulls at most one chat per frame
                                var chat = lines.ElementAt(last_chat);
                                if (!chat.Live && !VodChat)
                                {
                                    last_chat++;

                                }
                                else if (chat.StartFrame < i)
                                {
                                    drawable_messages.Push(chat);
                                    last_chat++;
                                }

                            }

                            DrawFrame(bmp, drawable_messages, i);
                            writer.WriteVideoFrame(bmp);
                        }


                        return true;
                    }
                }
            });


        }

        public static void DrawPreview(ViewModel vm, Bitmap bmp)
        {
            using (var chat = new ChatVideo(vm))
            {
                var messages = ChatHandler.MakeSampleChat(chat);
                chat.DrawFrame(bmp, messages);
                foreach (var msg in messages)
                {
                    msg.Lines.ForEach(m => m.Drawables.ForEach(d => d.Dispose()));
                }
            }
        }

        /// <summary>
        /// Draws a list of chat messages on a supplied Bitmap
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="lines"></param>
        /// <param name="frame"></param>
        public void DrawFrame(Bitmap bmp, Stack<DrawableMessage> lines, int frame = 0)
        {

            double height = 0;

            var messages = lines.TakeWhile(x => {
                var passes = height < (Height - VerticalPad * 2);
                height += x.Lines.LastOrDefault().Height + LineSpacing;
                return passes;
            });

            using (var drawing = Graphics.FromImage(bmp))
            {
                drawing.Clear(BGColor);

                float local_y = Height - VerticalPad;

                foreach (var message in messages)
                {
                    var last_line = message.Lines.LastOrDefault();
                    var message_y = local_y - (last_line.OffsetY + last_line.Height + LineSpacing);
                    local_y = message_y;

                    foreach (var line in message.Lines)
                    {
                        foreach (var drawable in line.Drawables)
                        {
                            var x = line.OffsetX + drawable.OffsetX;
                            var y = line.OffsetY + drawable.OffsetY + message_y;

                            if (drawable is User)
                            {
                                var user = drawable as User;
                                drawing.DrawString(user.Name, user.Font, user.Brush, x, y);

                            }
                            else if (drawable is Badge)
                            {
                                var badge = drawable as Badge;
                                drawing.DrawImage(badge.Image, new RectangleF(x, y, badge.Image.Width, badge.Image.Height));
                            }
                            else if (drawable is Text)
                            {
                                var msg = drawable as Text;
                                drawing.DrawString(msg.Message, msg.Font, msg.Brush, x, y);
                            }
                            else if (drawable is Emote)
                            {
                                var emote = drawable as Emote;
                                emote.SetFrame(frame);
                                drawing.DrawImage(emote.Image, new RectangleF(x, y, emote.Image.Width, emote.Image.Height));
                            }
                        }
                    }

                }

                using (var brush = new SolidBrush(BGColor))
                {
                    drawing.FillRectangle(brush, 0, 0, Width, VerticalPad);
                }
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
