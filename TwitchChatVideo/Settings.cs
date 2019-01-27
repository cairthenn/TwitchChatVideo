using Newtonsoft.Json;
using System.IO;
using System.Windows.Media;

namespace TwitchChatVideo
{
    public struct Settings
    {
        private const string path = "./settings.json";

        public static Settings Load()
        {
            if(File.Exists(path))
            {
                using (var f = File.OpenText(path))
                using (var r = new JsonTextReader(f))
                {
                    return new JsonSerializer().Deserialize<Settings>(r);
                }
            }

            return new Settings()
            {
                Width = 400,
                Height = 300,
                FontSize = 10,
                FontFamily = (FontFamily)new FontFamilyConverter().ConvertFromString("Arial"),
                BGColor = Color.FromRgb(0, 0, 0),
                ChatColor = Color.FromRgb(255, 255, 255),
                LineSpacing = 4,
                VodChat = false,
                ShowBadges = true,
            };
        }

        private Settings(ViewModel vm)
        {
            Width = vm.Width;
            Height = vm.Height;
            BGColor = vm.BGColor;
            ChatColor = vm.ChatColor;
            FontFamily = vm.FontFamily;
            FontSize = vm.FontSize;
            LineSpacing = vm.LineSpacing;
            ShowBadges = vm.ShowBadges;
            VodChat = vm.VodChat;
        }

        public static void Save(ViewModel vm)
        {
            var serializer = new JsonSerializer();

            using (var sw = new StreamWriter(path))
            using (var writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, new Settings(vm));
            }
        }

        public uint Width { get; set; }
        public uint Height { get; set; }
        public Color BGColor { get; set; }
        public Color ChatColor { get; set; }
        public FontFamily FontFamily { get; set; }
        public float FontSize { get; set; }
        public float LineSpacing { get; set; }
        public bool ShowBadges { get; set; }
        public bool VodChat { get; set; }
    }
}
