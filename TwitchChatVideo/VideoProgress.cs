using System.ComponentModel;

namespace TwitchChatVideo
{
    public struct VideoProgress
    {
        public enum VideoStatus
        {
            Idle,
            [Description("Downloading Video Info")]
            Info,
            [Description("Downloading Channel Badges")]
            Badges,
            [Description("Downloading Cheer Emotes")]
            Cheers,
            [Description("Downloading FFZ Emotes")]
            FFZ,
            [Description("Downloading BTTV Emotes")]
            BTTV,
            [Description("Downloading Chat History")]
            Chat,
            [Description("Determing Drawing Information")]
            Drawing,
            [Description("Rendering Video")]
            Rendering,
            [Description("Cleaning Up Resources")]
            CleaningUp,
        }

        public VideoStatus Status { get; }
        public long Progress { get; }
        public long Total { get; }

        public VideoProgress(long progress, long total, VideoStatus status)
        {
            Progress = progress;
            Total = total;
            Status = status;
        }

    }
}
