using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Compression;
using System.Diagnostics;

namespace TwitchChatVideo
{
    class Updater
    {
        public const string OldVersion = "old-version";
        public const string NewVersion = "new-version";

        public struct Update
        {
            public bool NewVersion;
            public string DownloadUrl;
        }

        public static async Task<Update> CheckForUpdates()
        {
            const string update_url = "https://api.github.com/repos/cairthenn/TwitchChatVideo/releases/latest";
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("User-Agent", "cairthenn");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                    var response = await client.GetAsync(update_url);
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    var releases = JObject.Parse(content);
                    var latest_version = new Version(releases["tag_name"].ToString().Substring(1));
                    if (latest_version.CompareTo(version) > 0)
                    {
                        return new Update() { NewVersion = true, DownloadUrl = releases["assets"].First["browser_download_url"].ToString() };
                    }

                    new Update() { NewVersion = false, DownloadUrl = releases["assets"].First["browser_download_url"].ToString() };
                }
                catch (HttpRequestException e)
                {
                    MessageBox.Show(string.Format("Unable to reach {0} \n\n{1}", update_url, e.Message));
                }

                return new Update() { NewVersion = false };
            }
        }

        public static async Task RunUpdate(string remote)
        {

            if (await DownloadUpdate(remote, NewVersion))
            {
                ZipFile.ExtractToDirectory($"{NewVersion}.zip", NewVersion);
                Directory.CreateDirectory(OldVersion);
                foreach(var file in Directory.GetFiles(NewVersion))
                {
                    var dest = file.Substring(file.IndexOf('\\') + 1);
                    File.Move(dest, $"{OldVersion}/{dest}");
                    File.Move(file, dest);
                }

                Directory.Delete(NewVersion, true);
                File.Delete($"{NewVersion}.zip");
                Process.Start(Assembly.GetExecutingAssembly().Location);
                Environment.Exit(-1);
            }
        }

        public static async Task<bool> DownloadUpdate(string url, string local)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStreamAsync();
                    using (var fo = File.Create($"{local}.zip"))
                    {
                        content.CopyTo(fo);
                        fo.Close();
                        return true;
                    }
                }
                catch (HttpRequestException e)
                {
                    MessageBox.Show(string.Format("Unable to downlod updates \n\n{0}", e.Message));
                }
            }

            return false;
        }
    }
}
