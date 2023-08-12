using Goog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoogLib
{
    public sealed class ModListProfile : ConfigFile<ModListProfile>
    {
        private List<string> _modlist = new List<string>();
        private string _syncURL = string.Empty;

        private ModListProfile()
        { }

        public List<string> Modlist { get => _modlist; set => _modlist = value; }

        [JsonIgnore]
        public string ProfileName => Path.GetFileNameWithoutExtension(FilePath) ?? string.Empty;

        public string SyncURL { get => _syncURL; set => _syncURL = value; }

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

        public static void ResolveProfile(Config config, ref string profileName)
        {
            string path = GetPath(config, profileName);
            if (File.Exists(path)) return;

            profileName = Tools.GetFirstFileName(Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderModlistProfiles), "*.json");
            if (!string.IsNullOrEmpty(profileName)) return;

            profileName = "Default";
            CreateFile(GetPath(config, profileName)).SaveFile();
        }

        public static bool TryParseModID(string path, out string id)
        {
            id = string.Empty;
            if (long.TryParse(path, out _))
            {
                id = path;
                return true;
            }

            if (Path.GetExtension(path) == ".pak")
                return TryParseFile2ModID(path, out id);
            else
                return TryParseDirectory2ModID(path, out id);
        }

        public static bool TryParseFile2ModID(string path, out string id)
        {
            id = string.Empty;

            string? folder = Path.GetDirectoryName(path);
            if (folder == null)
                return false;
            if (!long.TryParse(Path.GetFileName(folder), out _))
                return false;
            id = Path.GetFileName(folder);
            return true;
        }

        public static bool TryParseDirectory2ModID(string path, out string id)
        {
            id = string.Empty;
            if(long.TryParse(Path.GetFileName(path), out _))
            {
                id = Path.GetFileName(path);
                return true;
            }

            string? parent = Path.GetDirectoryName(path);
            if(parent != null && long.TryParse(Path.GetFileName(parent), out _))
            {
                id = Path.GetFileName(parent);
                return true;
            }
            return false;                
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
    }
}