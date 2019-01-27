# Twitch Chat Video

Create a chat replay video for any Twitch vod. Currently, a build of the project is available [here](https://cairthenn.com/twitch/TwitchChatVideo.zip).

Here's an example output at 300x300. YouTube stretches this to their player, so it's a bit blurry:
[![Click to view an example](http://img.youtube.com/vi/Qa3bVrpRb1U/0.jpg)](http://www.youtube.com/watch?v=Qa3bVrpRb1U)

This program requires .NET Framework Version 4.7.2. You may need to [install a newer version of .NET Framework](https://dotnet.microsoft.com/download/dotnet-framework-runtime) before running it.

## Usage

Enter the URL of a video and then click make video. It should look like this: `https://www.twitch.tv/videos/<Video ID>`. You may also simply use the Video ID. This process *can* take a long time, especially for long videos with many chat messages. The progress bar will let you know what's currently being worked on; when everything is done, a video will be published to the `output` directory.

The preview window provides an example of what your render will look like. Double click the preview image for a true to size preview!

## Options 

### Background Color
The background color to use for the video.

### Chat Color
The color to use for chat messages. Note that usernames and bits will always be drawn with the appropriate color.

### Font
The font to use for chat messages. Any font installed onto your system is available.

### Font Size
The font size to use for chat messages.

### Video Width
The horiztonal resolution of resulting video.

### Video Height
The vertical resolution of resulting video.

### Badges
Enable or disable user chat badges.

### Vod Chat
If enabled, chat posted by users during a vod will be included.

### Line Spacing
Inserts additional padding between chats by different users.

## Help!

If something doesn't seem to be working properly, you have something you'd like to see added, or you simply know way more about all of this than me and have some feedback, feel free to [create an issue](https://github.com/cairface/TwitchChatVideo/issues).

## Notes

Several small image files will be downloaded during the video creation process and stored in the `badges`, `emotes`, and `cheers` directories. This is mostly due to a limitation in .NET relating to animating GIFs, but comes with the benefit of not having to fetch the image multiple times over several videos. If desired, they can be deleted between videos and any needed images will be fetched again.

Similarly, a log file for each video will be created and published to `logs`. Downloading chat from Twitch takes a long time, so if there is a log file available, it will be used instead. If for some reason you wish to fetch a new version of chat (to include new vod comments, for example), simply delete the appropriate log file.
