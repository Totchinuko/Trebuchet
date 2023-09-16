﻿using System.Reflection;

namespace Trebuchet
{
    public static class AutoUpdateStatus
    {
        public const int CheckForUpdates = 2;
        public const int Never = 0;
        public const int OnlyOnStart = 1;
    }

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
        public const string FileBuildID = "buildid";
        public const string FileClientBEBin = "ConanSandbox_BE.exe";
        public const string FileClientBin = "ConanSandbox.exe";
        public const string FileConfig = "Config.json";
        public const string FileGeneratedModlist = "modlist.txt";
        public const string FileIniBase = "Engine\\Config\\Base{0}.ini";
        public const string FileIniDefault = "ConanSandbox\\Config\\Default{0}.ini";
        public const string FileIniServer = "ConanSandbox\\Saved\\Config\\WindowsServer\\{0}.ini";
        public const string FileIniUser = "ConanSandbox\\Saved\\Config\\WindowsNoEditor\\{0}.ini";
        public const string FileMapJson = "Json\\Maps.json";
        public const string FileProfileConfig = "profile.json";
        public const string FileServerBin = "ConanSandboxServer-Win64-Shipping.exe";
        public const string FileServerProxyBin = "ConanSandboxServer.exe";
        public const string FileTrebuchetLaunch = "trebuchet.json";
        public const string FolderClientProfiles = "ClientProfiles";
        public const string FolderGameBinaries = "ConanSandbox\\Binaries\\Win64";
        public const string FolderGameSave = "ConanSandbox\\Saved";
        public const string FolderInstancePattern = "Instance_{0}";
        public const string FolderLive = "Live";
        public const string FolderModlistProfiles = "Modlists";
        public const string FolderServerInstances = "ServerInstances";
        public const string FolderServerProfiles = "ServerProfiles";
        public const string FolderTestLive = "TestLive";
        public const string FolderWorkshop = "Workshop";
        public const string GameArgsLog = "-log";
        public const string GameArgsModList = "-modlist=\"{0}\"";
        public const string GameArgsUseAllCore = "-useallavailablecores";
        public const string ServerArgsMaxPlayers = "-MaxPlayers={0}";
        public const string ServerArgsMultiHome = "-MULTIHOME={0}";

        #endregion constants

        public int AutoUpdateStatus { get; set; } = 1;

        public uint ClientAppID => IsTestLive ? AppIDTestLiveClient : AppIDLiveClient;

        public string ClientPath { get; set; } = string.Empty;

        public string InstallPath { get; set; } = string.Empty;

        public bool IsInstallPathValid => !string.IsNullOrEmpty(InstallPath) && Directory.Exists(InstallPath);

        public bool IsTestLive => Path.GetFileName(FilePath) == $"{FolderTestLive}.{FileConfig}";

        public int MaxDownloads { get; set; } = 8;

        public uint ServerAppID => IsTestLive ? AppIDTestLiveServer : AppIDLiveServer;

        public int ServerInstanceCount { get; set; } = 0;

        public int UpdateCheckInterval { get; set; } = 300;

        public bool VerifyAll { get; set; } = false;

        public string VersionFolder => IsTestLive ? FolderTestLive : FolderLive;

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