using Goog;
using System.Text.Json;

namespace GoogLib
{
    public class ModListProfile : IFile
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true
        };

        private List<string> _modlist = new List<string>();
        private string _profileFile = string.Empty;
        private string _syncURL = string.Empty;

        public string FilePath { get => _profileFile; set => _profileFile = value; }

        public List<string> Modlist { get => _modlist; set => _modlist = value; }

        public string SyncURL { get => _syncURL; set => _syncURL = value; }

        public static string GetModlistPath(Config config, string modlistName)
        {
            return Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderModlistProfiles, modlistName + ".json");
        }

        public async Task DownloadModList(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_syncURL))
                throw new ArgumentException("Sync URL is invalid");

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);

                using (MemoryStream stream = new MemoryStream(10 * 1024))
                {
                    await client.DownloadAsync(_syncURL, stream, null, cancellationToken);

                    using (StreamReader sr = new StreamReader(stream))
                    {
                        string json = sr.ReadToEnd();
                        Modlist = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    }
                }
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

        public static void TryParseModList(ref List<string> modlist)
        {
            for(int i = 0; i < modlist.Count; i++)
            {
                if (TryParseModID(modlist[i], out string id))
                    modlist[i] = id;
            }
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
    }
}