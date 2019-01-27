using System.Windows;
using System.IO;

namespace TwitchChatVideo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(TwitchEmote.BaseDir);
            Directory.CreateDirectory(Badges.BaseDir);
            Directory.CreateDirectory(Bits.BaseDir);
            Directory.CreateDirectory(FFZ.BaseDir);
            Directory.CreateDirectory(BTTV.BaseDir);
            Directory.CreateDirectory(ChatVideo.OutputDirectory);
            Directory.CreateDirectory(ChatVideo.LogDirectory);
        }
    }
}
