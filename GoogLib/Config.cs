using System.Reflection;

namespace Goog
{
    public sealed class Config : ConfigFile<Config>
    {
        #region constants

        public const uint AppIDLiveClient = 440900;
        public const uint AppIDLiveServer = 443030;
        public const uint AppIDTestLiveClient = 931180;
        public const uint AppIDTestLiveServer = 931580;
        public const string CmdArgAppUpdate = "+app_update {0}";
        public const string CmdArgForceInstallDir = "+force_install_dir \"{0}\"";
        public const string CmdArgLogin = "+login \"{0}\" \"{1}\"";
        public const string CmdArgLoginAnonymous = "+login anonymous";
        public const string CmdArgQuit = "+quit";
        public const string CmdArgWorkshopUpdate = "+workshop_download_item {0} {1}";
        public const string FileClientBEBin = "ConanSandbox_BE.exe";
        public const string FileClientBin = "ConanSandbox.exe";
        public const string FileConfig = "Config.json";
        public const string FileGeneratedModlist = "modlist.txt";
        public const string FileIniBase = "Engine\\Config\\Base{0}.ini";
        public const string FileIniDefault = "ConanSandbox\\Config\\Default{0}.ini";
        public const string FileIniUser = "ConanSandbox\\Saved\\Config\\WindowsNoEditor\\{0}.ini";
        public const string FileMapJson = "Json\\Maps.json";
        public const string FileProfileConfig = "profile.json";
        public const string FileServerBin = "ConanSandboxServer-Win64-Shipping.exe";
        public const string FileServerProxyBin = "ConanSandboxServer.exe";
        public const string FileSteamAppInfo = "appcache\\appinfo.vdf";
        public const string FileSteamCMDBin = "steamcmd.exe";
        public const string FileSteamInstanceManifeste = "steamapps\\appmanifest_{0}.acf";
        public const string FileTrebuchetLaunch = "trebuchet.json";
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
        public const string GameArgsModList = "-modlist=\"{0}\"";
        public const string GameArgsUseAllCore = "-useallavailablecores";
        public const string ServerArgsMaxPlayers = "-MaxPlayers={0}";

        #endregion constants

        public uint ClientAppID => IsTestLive ? AppIDTestLiveClient : AppIDLiveClient;

        public string ClientPath { get; set; } = string.Empty;

        public bool DisplayCMD { get; set; } = false;

        public string InstallPath { get; set; } = string.Empty;

        public bool IsInstallPathValid => !string.IsNullOrEmpty(InstallPath) && Directory.Exists(InstallPath);

        public bool IsTestLive => Path.GetFileName(Path.GetDirectoryName(FilePath)) == FolderTestLive;

        public bool KillZombies { get; set; } = false;

        public bool RestartWhenDown { get; set; } = false;

        public uint ServerAppID => IsTestLive ? AppIDTestLiveServer : AppIDLiveServer;

        public int ServerInstanceCount { get; set; } = 0;

        public string VersionFolder => IsTestLive ? FolderTestLive : FolderLive;

        public int ZombieCheckSeconds { get; set; } = 300;

        public static string GetPath(bool testlive)
        {
            string? ConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(ConfigPath))
                throw new Exception("Path to assembly is invalid.");
            ConfigPath = Path.Combine(ConfigPath, $"{(testlive ? FolderTestLive : FolderLive)}.{FileConfig}");
            return ConfigPath;
        }
    }
}