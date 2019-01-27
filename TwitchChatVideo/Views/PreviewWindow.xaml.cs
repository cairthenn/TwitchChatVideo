using System.Windows;

namespace TwitchChatVideo
{
    /// <summary>
    /// Interaction logic for PreviewWindow.xaml
    /// </summary>
    public partial class PreviewWindow : Window
    {
        public PreviewWindow(ViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
