using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goog
{
    public sealed class Config
    {
        #region constants

        public const string AppIDLiveClient = "440900";
        public const string AppIDLiveServer = "443030";
        public const string AppIDTestLiveClient = "931180";
        public const string AppIDTestLiveServer = "931580";
        public const string CmdArgAppUpdate = "+app_update {0}";
        public const string CmdArgForceInstallDir = "+force_install_dir {0}";
        public const string CmdArgLogin = "+login {0} {1}";
        public const string CmdArgLoginAnonymous = "+login anonymous";
        public const string CmdArgQuit = "+quit";
        public const string CmdArgWorkshopUpdate = "+workshop_download_item {0} {1}";
        public const string FileClientBEBin = "ConanSandbox_BE.exe";
        public const string FileClientBin = "ConanSandbox.exe";
        public const string FileConfig = "Config.json";
        public const string FileGeneratedModlist = "modlist.txt";
        public const string FileProfileConfig = "profile.json";
        public const string FileServerBin = "ConanSandboxServer-Win64-Shipping.exe";
        public const string FileSteamCMDBin = "steamcmd.exe";
        public const string FolderClientProfiles = "ClientProfiles";
        public const string FolderGameBinaries = "ConanSandbox\\Binaries\\Win64";
        public const string FolderGameSave = "ConanSandbox\\Saved";
        public const string FolderLive = "Live";
        public const string FolderModlistProfiles = "Modlists";
        public const string FolderServerInstances = "ServerInstances";
        public const string FolderServerProfiles = "ServerProfiles";
        public const string FolderSteam = "Steam";
        public const string FolderSteamMods = "steamapps\\workshop\\content";
        public const string FolderTestLive = "TestLive";
        public const string GameArgsLog = "-log";
        public const string GameArgsModList = "-modlist={0}";
        public const string GameArgsUseAllCore = "-useallavailablecores";
        public const string ServerArgsMaxPlayers = "-MaxPlayers={0}";

        #endregion constants

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true
        };

        private string _clientPath = string.Empty;
        private string _currentClientProfile = string.Empty;
        private string _currentModlistProfile = string.Empty;
        private string _currentServerProfile = string.Empty;
        private string _installPath = string.Empty;
        private int _serverInstanceCount = 0;

        public Config()
        {
            if (!ServerProfileExists(_currentServerProfile))
                TryGetFirstProfile(out _currentServerProfile);
        }

        public string ClientAppID => IsTestLive ? AppIDTestLiveClient : AppIDLiveClient;

        public string ClientPath { get => _clientPath; set => _clientPath = value; }

        public string CurrentServerProfile { get => _currentServerProfile; set => _currentServerProfile = value; }

        public string InstallPath { get => _installPath; set => _installPath = value; }

        public bool IsInstallPathValid => !string.IsNullOrEmpty(_installPath) && Directory.Exists(_installPath);

        [JsonIgnore]
        public bool IsTestLive { get; private set; }

        public string ServerAppID => IsTestLive ? AppIDTestLiveServer : AppIDLiveServer;

        public int ServerInstanceCount { get => _serverInstanceCount; set => _serverInstanceCount = value; }

        public string VersionFolder => IsTestLive ? FolderTestLive : FolderLive;

        public static string GetConfigPath(bool testlive)
        {
            string? ConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(ConfigPath))
                throw new Exception("Path to assembly is invalid.");
            ConfigPath = Path.Combine(ConfigPath, (testlive ? FolderTestLive : FolderLive), FileConfig);
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

            Config = JsonSerializer.Deserialize<Config>(json, _jsonOptions) ?? new Config();
            Config.IsTestLive = testlive;
        }

        public bool ClientProfileExists(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return false;
            return File.Exists(Path.Combine(InstallPath, VersionFolder, FolderClientProfiles, profileName, FileProfileConfig));
        }

        public void CreateInstanceDirectories()
        {
            if (ServerInstanceCount <= 0) return;
            string instancesFolder = Path.Combine(InstallPath, VersionFolder, FolderServerInstances);
            Tools.CreateDir(instancesFolder);
            for (int i = 1; i <= ServerInstanceCount; i++)
            {
                string instance = Path.Combine(instancesFolder, "Instance_" + i);
                Tools.CreateDir(instance);
            }
        }

        public List<string> GetAllProfiles()
        {
            string folder = Path.Combine(InstallPath, VersionFolder, FolderServerProfiles);
            if (!Directory.Exists(Path.Combine(InstallPath, VersionFolder, FolderServerProfiles)))
                return new List<string>();
            List<string> profiles = Directory.GetDirectories(folder).ToList();
            for (int i = 0; i < profiles.Count; i++)
                profiles[i] = Path.GetFileName(profiles[i]);
            return profiles;
        }

        public void RemoveAllSymbolicLinks()
        {
            string folder = Path.Combine(InstallPath, VersionFolder, FolderServerInstances);
            string[] instances = Directory.GetDirectories(folder);
            foreach (string instance in instances)
                Tools.RemoveSymboliclink(Path.Combine(instance, FolderGameSave));
        }

        public bool ResolveMod(ref string mod)
        {
            string file = mod;
            if (long.TryParse(mod, out _))
                file = Path.Combine(InstallPath, FolderSteam, FolderSteamMods, mod, "none");

            string? folder = Path.GetDirectoryName(file);
            if (folder == null)
                return false;

            if (!long.TryParse(Path.GetFileName(folder), out _))
                return File.Exists(file);

            if (!Directory.Exists(folder))
                return false;

            string[] files = Directory.GetFiles(folder, "*.pak", SearchOption.TopDirectoryOnly);
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
            string json = JsonSerializer.Serialize(this, _jsonOptions);
            string ConfigPath = GetConfigPath(IsTestLive);

            File.WriteAllText(ConfigPath, json);
        }

        public bool ServerProfileExists(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return false;
            return File.Exists(Path.Combine(InstallPath, VersionFolder, FolderServerProfiles, profileName, FileProfileConfig));
        }

        public bool TryGetFirstProfile(out string profileName)
        {
            profileName = string.Empty;
            string folder = Path.Combine(InstallPath, VersionFolder, FolderServerProfiles);
            if (!Directory.Exists(folder))
                return false;
            string[] directories = Directory.GetDirectories(folder);
            if (directories.Length == 0)
                return false;
            if (directories.Contains(Path.Combine(folder, "Default")))
            {
                profileName = "Default";
                return true;
            }
            profileName = Path.GetFileName(directories[0]);
            return !string.IsNullOrEmpty(profileName);
        }
    }
}