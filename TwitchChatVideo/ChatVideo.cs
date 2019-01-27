using Accord.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        private float Spacing { get; set; }
        private Color BGColor { get; set; }
        private Color ChatColor { get; set; }
        private float Width { get; set; }
        private float Height { get; set; }
        private Font Font { get; set; }
        private bool UseBadges { get; set; }
        private bool VodChat { get; set; }

        private ViewModel VM { get; }

        public ChatVideo(ViewModel vm)
        {
            VM = vm;
            ID = vm.URL?.Split('/').LastOrDefault() ?? vm.URL ?? "";
            Spacing = vm.LineSpacing;
            BGColor = Color.FromArgb(vm.BGColor.A, vm.BGColor.R, vm.BGColor.G, vm.BGColor.B);
            ChatColor = Color.FromArgb(vm.ChatColor.A, vm.ChatColor.R, vm.ChatColor.G, vm.ChatColor.B);
            Width = vm.Width;
            Height = vm.Height;
            Font = new Font(vm.FontFamily.ToString(), vm.FontSize);
            VodChat = vm.VodChat;
            UseBadges = vm.ShowBadges;
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

                if (video.ID == string.Empty)
                {
                    return false;
                }

                var bits = await TwitchDownloader.DownloadBitsAsync(video.StreamerID, progress, ct);
                var badges = await TwitchDownloader.DownloadBadgesAsync(video.StreamerID, progress, ct);
                var bttv = await BTTV.CreateAsync(video.Streamer, progress, ct);
                var ffz = await FFZ.CreateAsync(video.Streamer, progress, ct);
                var messages = await TwitchDownloader.GetChatAsync(ID, video.Duration, progress, ct);

                using (var chat_handler = new ChatHandler(VM, bttv, ffz, badges, bits))
                {
                    int current = 0;

                    var drawables = messages.Select(m =>
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
                    writer.Open(path, (int)Width, (int)Height, FPS, Codec);
                    using (var bmp = new Bitmap((int)Width, (int)Height))
                    {
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
            var messages = ChatHandler.MakeSampleChat(vm);
            using (var chat = new ChatVideo(vm))
            {
                chat.DrawFrame(bmp, messages);
            }
            foreach (var msg in messages)
            {
                msg.Lines.ForEach(m => m.Drawables.ForEach(d => d.Dispose()));
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
                height += x.Lines.LastOrDefault().Height + Spacing;
                return passes;
            });

            using (var drawing = Graphics.FromImage(bmp))
            {
                drawing.Clear(BGColor);

                float local_y = Height - VerticalPad;

                foreach (var message in messages)
                {
                    var last_line = message.Lines.LastOrDefault();
                    var message_y = local_y - (last_line.OffsetY + last_line.Height + Spacing);
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
