using GoogLib;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Goog
{
    public sealed class Config : ConfigFile<Config>
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
        public const string FolderInstancePattern = "Instance_{0}";
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
        public const string FileIniDefault = "ConanSandbox\\Config\\Default{0}.ini";
        public const string FileIniBase = "Engine\\Config\\Base{0}.ini";
        public const string FileIniUser = "ConanSandbox\\Saved\\Config\\WindowsNoEditor\\{0}.ini";

        #endregion constants

        private string _clientPath = string.Empty;
        private bool _displayCMD = false;
        private string _installPath = string.Empty;
        private int _serverInstanceCount = 0;
        private string _steamAPIKey = string.Empty;
        private PastLaunch? _clientPastLaunch = null;
        private PastLaunch?[] _serverPastLaunch = new PastLaunch[0];

        public string ClientAppID => IsTestLive ? AppIDTestLiveClient : AppIDLiveClient;

        public string ClientPath { get => _clientPath; set => _clientPath = value; }

        public bool DisplayCMD { get => _displayCMD; set => _displayCMD = value; }

        public string InstallPath { get => _installPath; set => _installPath = value; }

        public bool IsInstallPathValid => !string.IsNullOrEmpty(_installPath) && Directory.Exists(_installPath);

        public bool IsTestLive => Path.GetFileName(Path.GetDirectoryName(FilePath)) == FolderTestLive;

        public string ServerAppID => IsTestLive ? AppIDTestLiveServer : AppIDLiveServer;

        public int ServerInstanceCount { get => _serverInstanceCount; set => _serverInstanceCount = value; }

        public string SteamAPIKey { get => _steamAPIKey; set => _steamAPIKey = value; }

        public string VersionFolder => IsTestLive ? FolderTestLive : FolderLive;

        public PastLaunch? ClientPastLaunch { get => _clientPastLaunch; set => _clientPastLaunch = value; }

        public static string GetPath(bool testlive)
        {
            string? ConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(ConfigPath))
                throw new Exception("Path to assembly is invalid.");
            ConfigPath = Path.Combine(ConfigPath, $"{(testlive ? FolderTestLive : FolderLive)}.{FileConfig}");
            return ConfigPath;
        }

        public string GetInstancePath(int instance)
        {
            return Path.Combine(InstallPath, VersionFolder, FolderServerInstances, string.Format(FolderInstancePattern, instance));
        }

        public void CreateInstanceDirectories()
        {
            if (ServerInstanceCount <= 0) return;
            string instancesFolder = Path.Combine(InstallPath, VersionFolder, FolderServerInstances);
            Tools.CreateDir(instancesFolder);
            for (int i = 1; i <= ServerInstanceCount; i++)
            {
                string instance = Path.Combine(instancesFolder, string.Format(FolderInstancePattern, i));
                Tools.CreateDir(instance);
            }
        }

        public int GetInstalledInstances()
        {
            int count = 0;

            string folder = Path.Combine(InstallPath, VersionFolder, FolderServerInstances);
            if (!Directory.Exists(folder))
                return 0;

            string[] instances = Directory.GetDirectories(folder);
            foreach (string instance in instances)
            {
                string bin = Path.Combine(instance, FolderGameBinaries, FileServerBin);
                if (File.Exists(bin))
                    count++;
            }

            return count;
        }

        public void RemoveAllSymbolicLinks()
        {
            string folder = Path.Combine(InstallPath, VersionFolder, FolderServerInstances);
            if (!Directory.Exists(folder))
                return;
            string[] instances = Directory.GetDirectories(folder);
            foreach (string instance in instances)
                Tools.RemoveSymboliclink(Path.Combine(instance, FolderGameSave));
        }

        public bool ResolveMod(ref string mod)
        {
            string file = mod;
            if (long.TryParse(mod, out _))
                file = Path.Combine(InstallPath, FolderSteam, FolderSteamMods, ClientAppID, mod, "none");

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

        public void ResolveModsPath(List<string> modlist, out List<string> result, out List<string> errors)
        {
            result = new List<string>();
            errors = new List<string>();

            foreach (string mod in modlist)
            {
                string path = mod;
                if (!ResolveMod(ref path))
                    errors.Add(path);
                result.Add(path);
            }
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

        public void SetServerPastLaunch(PastLaunch? pastLaunch, int instance)
        {
            if (_serverPastLaunch.Length <= instance)
                Array.Resize(ref _serverPastLaunch, instance + 1);
            _serverPastLaunch[instance] = pastLaunch;
        }

        public bool TryGetServerPastLaunch(int instance, [NotNullWhen(true)] out PastLaunch? pastLaunch)
        {
            pastLaunch = null;
            if (_serverPastLaunch.Length <= instance) return false;
            pastLaunch = _serverPastLaunch[instance];
            return pastLaunch != null;
        }
    }
}