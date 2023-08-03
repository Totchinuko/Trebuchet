using GoogLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Goog
{
    public class Config
    {
        public const string ConfigFilename = "Goog.json";

        private string _clientPath = string.Empty;
        private string _currentProfile = string.Empty;
        private string _installPath = string.Empty;
        private bool _managerServers = false;
        private List<ServerInstance> _serverInstances = new List<ServerInstance> { new ServerInstance() };

        public Config()
        {
            if (!ProfileExists(_currentProfile))
                TryGetFirstProfile(out _currentProfile);
        }

        public string ClientAppID => IsTestLive ? testLiveClientAppID : liveClientAppID;

        public string ClientPath
        {
            get => _clientPath;
            set => _clientPath = value;
        }

        public string CurrentProfile
        {
            get { return _currentProfile; }
            set { _currentProfile = value; }
        }

        public string InstallPath
        {
            get => _installPath;
            set => _installPath = value;
        }

        [JsonIgnore]
        public bool IsTestLive { get; private set; }

        public bool ManageServers
        {
            get => _managerServers;
            set => _managerServers = value;
        }

        public string ServerAppID => IsTestLive ? testLiveServerAppID : liveServerAppID;

        public List<ServerInstance> ServerInstances
        {
            get => _serverInstances;
            set => _serverInstances = value;
        }

        #region constants

        public const string clientBEBin = "ConanSandbox_BE.exe";
        public const string clientBin = "ConanSandbox.exe";
        public const string CmdArgAppUpdate = "+app_update {0}";
        public const string CmdArgForceInstallDir = "+force_install_dir {0}";
        public const string CmdArgLogin = "+login {0} {1}";
        public const string CmdArgLoginAnonymous = "+login anonymous";
        public const string CmdArgQuit = "+quit";
        public const string CmdArgWorkshopUpdate = "+workshop_download_item {0} {1}";
        public const string GameArgsLog = "-log";
        public const string GameArgsModList = "-modlist={0}";
        public const string GameArgsUseAllCore = "-useallavailablecores";
        public const string gameBinariesFolder = "ConanSandbox\\Binaries\\Win64";
        public const string gameSaveFolder = "ConanSandbox\\Saved";
        public const string liveClientAppID = "440900";
        public const string liveServerAppID = "443030";
        public const string liveServerFolder = "LiveServer";
        public const string profileConfigName = "profile.json";
        public const string profileFolder = "Profiles";
        public const string profileGeneratedModlist = "modlist.txt";
        public const string ServerArgsMaxPlayers = "-MaxPlayers={0}";
        public const string serverBin = "ConanSandboxServer-Win64-Shipping.exe";
        public const string steamCMDBin = "steamcmd.exe";
        public const string steamFolder = "Steam";
        public const string steamModFolder = "steamapps\\workshop\\content";
        public const string testLiveClientAppID = "931180";
        public const string testLiveServerAppID = "931580";
        public const string testLiveServerFolder = "TestLiveServer";
        public const string testProfileFolder = "TestLiveProfiles";

        #endregion constants

        #region Path

        public FileInfo ClientBEBin => new FileInfo(Path.Combine(ClientBinaries.FullName, clientBEBin));
        public FileInfo ClientBin => new FileInfo(Path.Combine(ClientBinaries.FullName, clientBin));
        public DirectoryInfo ClientBinaries => new DirectoryInfo(Path.Combine(ClientFolder.FullName, gameBinariesFolder));
        public DirectoryInfo ClientFolder => new DirectoryInfo(ClientPath);
        public DirectoryInfo ProfilesFolder => new DirectoryInfo(Path.Combine(InstallPath, IsTestLive ? testProfileFolder : profileFolder));
        public FileInfo ServerBin => new FileInfo(Path.Combine(ServerBinaryFolder.FullName, serverBin));
        public DirectoryInfo ServerBinaryFolder => new DirectoryInfo(Path.Combine(ServerFolder.FullName, gameBinariesFolder));
        public DirectoryInfo ServerFolder => new DirectoryInfo(Path.Combine(InstallPath, IsTestLive ? testLiveServerFolder : liveServerFolder));
        public DirectoryInfo ServerOriginalSaveFolder => new DirectoryInfo(Path.Combine(ServerFolder.FullName, gameSaveFolder + "_Original"));
        public DirectoryInfo ServerSaveFolder => new DirectoryInfo(Path.Combine(ServerFolder.FullName, gameSaveFolder));
        public FileInfo SteamCMD => new FileInfo(Path.Combine(SteamFolder.FullName, steamCMDBin));
        public DirectoryInfo SteamFolder => new DirectoryInfo(Path.Combine(InstallPath, steamFolder));
        public DirectoryInfo SteamModFolder => new DirectoryInfo(Path.Combine(SteamFolder.FullName, steamModFolder, ClientAppID));

        #endregion Path

        public static string? GetConfigPath(bool testlive)
        {
            string? ConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(ConfigPath))
                return null;
            ConfigPath = Path.Combine(ConfigPath, (testlive ? "TestLive." : "") + ConfigFilename);
            return ConfigPath;
        }

        public static void Load(out Config Config, bool testlive)
        {
            string ConfigPath = GetConfigPath(testlive) ?? "";
            string json = "";
            Config = new Config();
            Config.IsTestLive = testlive;

            if (!File.Exists(ConfigPath))
                return;
            json = File.ReadAllText(ConfigPath);
            if (string.IsNullOrEmpty(json))
                return;

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreReadOnlyProperties = true;
            Config = JsonSerializer.Deserialize<Config>(json, options) ?? new Config();
            Config.IsTestLive = testlive;
        }

        public bool ProfileExists(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return false;
            return File.Exists(Path.Combine(ProfilesFolder.FullName, profileName, profileConfigName));
        }

        public void ResolveMap(ref string map, Profile? profile = null)
        {
            if (!string.IsNullOrEmpty(map))
                return;

            if (!string.IsNullOrEmpty(profile?.Server.Map))
            {
                map = profile.Server.Map;
                SaveConfig();
                return;
            }

            throw new Exception("No map could be resolved");
        }

        public bool ResolveMod(ref string mod)
        {
            FileInfo file = new FileInfo(mod);

            if (long.TryParse(mod, out _))
                file = new FileInfo(Path.Combine(SteamModFolder.FullName, mod, "Unknown"));

            if (file.Directory == null)
                return false;

            if (!long.TryParse(file.Directory.Name, out _))
                return file.Exists;

            DirectoryInfo dir = new DirectoryInfo(Path.Combine(SteamModFolder.FullName, file.Directory.Name));
            if (!dir.Exists)
                return false;

            string[] files = Directory.GetFiles(dir.FullName, "*.pak", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
                return false;

            mod = files[0];
            return true;
        }

        public int ResolveModsPath(List<string> modlist, out List<string> resolved, out List<string> errors)
        {
            resolved = new List<string>();
            errors = new List<string>();

            int count = 0;
            foreach (string mod in modlist)
            {
                string path = mod;
                if (ResolveMod(ref path))
                    count++;
                else
                    errors.Add(path);
                resolved.Add(path);
            }

            return count;
        }

        public void SaveConfig()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreReadOnlyProperties = true;
            options.WriteIndented = true;
            string json = JsonSerializer.Serialize(this, options);
            string? ConfigPath = GetConfigPath(IsTestLive) ?? "";

            File.WriteAllText(ConfigPath, json);
        }

        public bool TryGetFirstProfile(out string profileName)
        {
            profileName = string.Empty;
            if (!ProfilesFolder.Exists)
                return false;
            string[] files = Directory.GetFiles(ProfilesFolder.FullName);
            if (files.Length == 0)
                return false;
            profileName = files[0];
            return true;
        }
    }
}