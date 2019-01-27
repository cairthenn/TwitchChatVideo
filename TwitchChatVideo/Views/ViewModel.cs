using Accord.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static TwitchChatVideo.VideoProgress;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;

namespace TwitchChatVideo
{
    public class ViewModel : INotifyPropertyChanged
    {
        private string url;
        private uint width;
        private uint height;
        private FontFamily font_family;
        private float font_size;
        private Color bg_color;
        private Color chat_color;
        private bool show_badges;
        private bool vod_chat;
        private float spacing;
        private bool running;
        private long total;
        private long progress;
        private VideoStatus status;

        private CancellationTokenSource CancellationTokenSource { get; set; }

        public long Total { get => total; set => Set(ref total, value, false); }
        public long Progress { get => progress; set => Set(ref progress, value, false); }
        public VideoStatus Status { get => status; set => Set(ref status, value, false); }
        public String URL{ get => url; set => Set(ref url, value, false); }

        public uint Width { get => width; set => Set(ref width, Math.Min(3000, value)); }
        public uint Height { get => height; set => Set(ref height, Math.Min(3000, value)); }
        public FontFamily FontFamily { get => font_family; set => Set(ref font_family, value); }
        public float FontSize { get => font_size; set => Set(ref font_size, value); }
        public Color BGColor { get => bg_color; set => Set(ref bg_color, value); }
        public Color ChatColor { get => chat_color; set => Set(ref chat_color, value); }
        public bool ShowBadges { get => show_badges; set => Set(ref show_badges, value); }
        public bool VodChat { get => vod_chat; set => Set(ref vod_chat, value); }
        public float LineSpacing { get => spacing; set => Set(ref spacing, value); }
        public bool Running { get => running; set => Set(ref running, value); }

        public ICommand CancelVideo { get; }
        public ICommand MakeVideo { get; }
        public ICommand MakePreviewWindow { get; }

        public BitmapSource PreviewImage {
            get {
                using (var bmp = new Bitmap((int)Width, (int)Height))
                {
                    ChatVideo.DrawPreview(this, bmp);
                    var hbmp = bmp.GetHbitmap();
                    var img_source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hbmp, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight((int)Width, (int)Height));
                    NativeMethods.DeleteObject(hbmp);
                    return img_source;
                }
            }
        }

        public class DelegateCommand : ICommand
        {

            private Func<object, bool> can_execute;
            private Action<object> execute;
            public event EventHandler CanExecuteChanged;

            public DelegateCommand(Action<object> execute) :
                this(execute, null)
            { }

            public DelegateCommand(Action<object> exe, Func<object, bool> ce)
            {
                execute = exe ?? throw new ArgumentNullException(nameof(exe));
                can_execute = ce;
            }

            public bool CanExecute(object param)
            {
                return can_execute?.Invoke(param) ?? true;
            }

            public void Execute(object param) => execute(param);

        }

        public ViewModel()
        {
            var settings = Settings.Load();
            Width = settings.Width;
            Height = settings.Height;
            LineSpacing = settings.LineSpacing;
            FontFamily = settings.FontFamily;
            FontSize = settings.FontSize;
            BGColor = settings.BGColor;
            ChatColor = settings.ChatColor;
            ShowBadges = settings.ShowBadges;
            VodChat = settings.VodChat;

            Total = 1;
            Progress = 0;

            MakeVideo = new DelegateCommand(ExecuteMakeVideo);
            CancelVideo = new DelegateCommand(ExecuteCancelVideo);
            MakePreviewWindow = new DelegateCommand(ExecuteMakePreviewWindow);
            Application.Current.MainWindow.Closing += new CancelEventHandler(SaveSettings);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string property_name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
        }

        protected void Set<T>(ref T field, T value, bool update_image = true, [CallerMemberName] string propertyName = null)
        {
            field = value;
            OnPropertyChanged(propertyName);
            if (update_image)
            {
                OnPropertyChanged("PreviewImage");
            }
        }

        private void ExecuteCancelVideo(object arg)
        {
            CancellationTokenSource?.Cancel();
        }

        private async void ExecuteMakeVideo(object arg)
        {
            Running = true;
            var c = new ChatVideo(this);
            using (var source = new CancellationTokenSource()) {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                CancellationTokenSource = source;
                var progress = new Progress<VideoProgress>(x =>
                {
                    Progress = x.Progress;
                    Total = x.Total;
                    Status = x.Status;
                });

                if(await c.CreateVideoAsync(progress, source.Token))
                {
                    sw.Stop();
                    var elapsed = sw.Elapsed;
                    URL = "";
                    MessageBox.Show(String.Format("Video completed in {0:00}:{1:00}:{2:00}!", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
                }

                Progress = 0;
                Total = 1;
                Status = VideoStatus.Idle;
                CancellationTokenSource = null;
                Running = false;
            }
        }

        private void ExecuteMakePreviewWindow(object arg)
        {
            new PreviewWindow(this).Show();
        }

        private void SaveSettings(object sender, CancelEventArgs e)
        {
            Settings.Save(this);
        }
    }
}
