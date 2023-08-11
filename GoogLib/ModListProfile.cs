using Goog;
using System.Text.Json;

namespace GoogLib
{
    public sealed class ModListProfile : ConfigFile<ModListProfile>
    {
        private List<string> _modlist = new List<string>();
        private string _syncURL = string.Empty;

        public List<string> Modlist { get => _modlist; set => _modlist = value; }

        public string SyncURL { get => _syncURL; set => _syncURL = value; }

        private ModListProfile() { }

        public static async Task<List<string>> DownloadModList(string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Sync URL is invalid");

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);

                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    var contentLength = response.Content.Headers.ContentLength;
                    if (response.Content.Headers.ContentLength > 1024 * 1024 * 10)
                        throw new Exception("Content was too big.");
                    if (response.Content.Headers.ContentType?.MediaType != "application/json")
                        throw new Exception("Content was not json.");

                    using (var download = await response.Content.ReadAsStreamAsync())
                    {
                        return await JsonSerializer.DeserializeAsync<List<string>>(download, _jsonOptions) ?? new List<string>();
                    }
                }
            }
        }

        public static string GetPath(Config config, string modlistName)
        {
            return Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderModlistProfiles, modlistName + ".json");
        }

        public static bool TryParseModID(string mod, out string id)
        {
            id = string.Empty;
            if (long.TryParse(mod, out _))
            {
                id = mod;
                return true;
            }
            FileInfo file = new FileInfo(Path.Combine(mod));
            if (file.Directory == null)
                return false;
            if (!long.TryParse(file.Directory.Name, out _))
                return false;
            id = file.Directory.Name;
            return true;
        }

        public static void TryParseModList(ref List<string> modlist)
        {
            for (int i = 0; i < modlist.Count; i++)
            {
                if (TryParseModID(modlist[i], out string id))
                    modlist[i] = id;
            }
        }

        public List<string> GetModIDList()
        {
            List<string> list = new List<string>();

            foreach (string mod in Modlist)
                if (TryParseModID(mod, out string id))
                    list.Add(id);

            return list;
        }

        public string GetModList()
        {
            return string.Join("\r\n", Modlist);
        }

        public void SetModList(string modlist)
        {
            Modlist = modlist.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static void ResolveProfile(Config config, ref string profileName)
        {
            string path = GetPath(config, profileName);
            if(File.Exists(path)) return;

            profileName = Tools.GetFirstFileName(Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderModlistProfiles), "*.json");
            if (!string.IsNullOrEmpty(profileName)) return;

            profileName = "Default";
            CreateFile(GetPath(config, profileName)).SaveFile();
        }

        public static List<string> ListProfiles(Config config)
        {
            List<string> list = new List<string>();
            string folder = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderModlistProfiles);
            if (!Directory.Exists(folder))
                return list;

            string[] profiles = Directory.GetFiles(folder, "*.json");
            foreach (string p in profiles)
                list.Add(Path.GetFileNameWithoutExtension(p));
            return list;
        }
    }
}