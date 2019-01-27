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
