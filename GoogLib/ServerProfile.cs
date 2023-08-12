using Goog;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoogLib
{
    public class ServerProfile : ConfigFile<ServerProfile>
    {
        private bool _killZombies = false;
        private bool _log = false;
        private string _map = "/Game/Maps/ConanSandbox/ConanSandbox";
        private int _maxPlayers = 30;
        private bool _restartWhenDown = false;
        private List<string> _sudoSuperAdmins = new List<string>();
        private bool _useAllCores = true;
        private int _zombieCheckSeconds = 300;

        public bool KillZombies { get => _killZombies; set => _killZombies = value; }

        public bool Log { get => _log; set => _log = value; }

        public string Map { get => _map; set => _map = value; }

        public int MaxPlayers { get => _maxPlayers; set => _maxPlayers = value; }

        public bool RestartWhenDown { get => _restartWhenDown; set => _restartWhenDown = value; }

        public List<string> SudoSuperAdmins { get => _sudoSuperAdmins; set => _sudoSuperAdmins = value; }

        public bool UseAllCores { get => _useAllCores; set => _useAllCores = value; }

        public int ZombieCheckSeconds { get => _zombieCheckSeconds; set => _zombieCheckSeconds = value; }

        [JsonIgnore]
        public string ProfileName => Path.GetFileName(Path.GetDirectoryName(FilePath)) ?? string.Empty;

        public static string GetPath(Config config, string name) => Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerProfiles, name, Config.FileProfileConfig);

        public static void ResolveProfile(Config config, ref string profileName)
        {
            if(!string.IsNullOrEmpty(profileName))
            {
                string path = GetPath(config, profileName);
                if (File.Exists(path)) return;
            }

            profileName = Tools.GetFirstFileName(Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerProfiles), "*");
            if (!string.IsNullOrEmpty(profileName)) return;

            profileName = "Default";
            if (!File.Exists(GetPath(config, profileName)))
                CreateFile(GetPath(config, profileName)).SaveFile();
        }

        public static List<string> ListProfiles(Config config)
        {
            List<string> list = new List<string>();
            string folder = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerProfiles);
            if (!Directory.Exists(folder))
                return list;

            string[] profiles = Directory.GetDirectories(folder, "*");
            foreach (string p in profiles)
                list.Add(Path.GetFileName(p));
            return list;
        }

        public static Dictionary<string, string> GetMapList()
        {
            string? appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(appFolder)) throw new Exception("Path to assembly is invalid.");

            string file = Path.Combine(appFolder, Config.FileMapJson);
            if (!File.Exists(file)) throw new Exception("Map list file is missing.");

            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
            if (data == null) throw new Exception("Map list could ne be parsed.");

            return data;
        }
    }
}