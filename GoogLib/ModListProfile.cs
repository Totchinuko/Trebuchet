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

        public static async Task<ModlistExport> DownloadModList(string url, CancellationToken cancellationToken)
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
                        return await JsonSerializer.DeserializeAsync<ModlistExport>(download, _jsonOptions) ?? new ModlistExport();
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
            if(!string.IsNullOrEmpty(profileName))
            {
                string path = GetPath(config, profileName);
                if (File.Exists(path)) return;
            }

            profileName = Tools.GetFirstFileName(Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderModlistProfiles), "*.json");
            if (!string.IsNullOrEmpty(profileName)) return;

            profileName = "Default";
            if (!File.Exists(GetPath(config, profileName)))
                CreateFile(GetPath(config, profileName)).SaveFile();
        }

        public static bool TryParseModID(string path, out ulong id)
        {
            id = 0;
            if (ulong.TryParse(path, out id))
                return true;

            if (Path.GetExtension(path) == ".pak")
                return TryParseFile2ModID(path, out id);
            else
                return TryParseDirectory2ModID(path, out id);
        }

        public static bool TryParseFile2ModID(string path, out ulong id)
        {
            id = 0;
            string? folder = Path.GetDirectoryName(path);
            if (folder == null)
                return false;
            if (ulong.TryParse(Path.GetFileName(folder), out id))
                return true;

            return false;
        }

        public static bool TryParseDirectory2ModID(string path, out ulong id)
        {
            id = 0;
            if(ulong.TryParse(Path.GetFileName(path), out id))
                return true;

            string? parent = Path.GetDirectoryName(path);
            if(parent != null && ulong.TryParse(Path.GetFileName(parent), out id))
                return true;

            return false;                
        }

        public static IEnumerable<string> ParseModList(IEnumerable<string> modlist)
        {
            foreach (var mod in modlist)
                if (TryParseModID(mod, out ulong id))
                    yield return id.ToString();
                else
                    yield return mod;
        }

        public IEnumerable<ulong> GetModIDList()
        {
            return GetModIDList(Modlist);
        }

        public static IEnumerable<ulong> GetModIDList(IEnumerable<string> modlist)
        {
            foreach (string mod in modlist)
                if (TryParseModID(mod, out ulong id))
                    yield return id;
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