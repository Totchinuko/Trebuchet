using tot_lib;

namespace TrebuchetLib;

public static class Constants
{
    public const uint AppIDLiveClient = 440900;
    public const uint AppIDLiveServer = 443030;
    public const uint AppIDTestLiveClient = 931180;
    public const uint AppIDTestLiveServer = 931580;
    public const string FileBuildID = "buildid";
    public const string FileClientBEBin = "ConanSandbox_BE.exe";
    public const string FileClientBin = "ConanSandbox.exe";
    public const string FileLiveConfig = "settings.live.json";
    public const string FileTestLiveConfig = "settings.testlive.json";
    public const string FileGeneratedModlist = "modlist.txt";
    public const string FileIniBase = "Engine\\Config\\Base{0}.ini";
    public const string FileIniDefault = "ConanSandbox\\Config\\Default{0}.ini";
    public const string FileIniServer = "ConanSandbox\\Saved\\Config\\WindowsServer\\{0}.ini";
    public const string FileIniUser = "ConanSandbox\\Saved\\Config\\WindowsNoEditor\\{0}.ini";
    public const string FileMapJson = "maps.json";
    public const string FileProfileConfig = "profile.json";
    public const string FileServerBin = "ConanSandboxServer-Win64-Shipping.exe";
    public const string FileServerProxyBin = "ConanSandboxServer.exe";
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
    public const string RegexSavedFolder = @"ConanSandbox([\\/]+)Saved";
    public const string ServerArgsMaxPlayers = "-MaxPlayers={0}";
    public const string ServerArgsMultiHome = "-MULTIHOME={0}";
    public const string SteamWorkshopURL = "https://steamcommunity.com/sharedfiles/filedetails/?id={0}";
    public const string GamePrimaryJunction = "GameSaved";
    public const string GameEmptyJunction = "EmptyGame";
    public const string JsonExt = "json";
    public const string PakExt = "pak";
    public const string TxtExt = "txt";
    
    public const string BoulderExe = "boulder.exe";
    public const string SteamClientExe = "steam.exe";
    
    public const string argLive = "--live";
    public const string argTestLive = "--testlive";
    public const string argCatapult = "--catapult";
    public const string argExperiment = "--experiment";
    public const string argBoulderSave = "--save";
    public const string argBoulderInstance = "--instance";
    public const string argBoulderModlist = "--modlist";
    public const string argBoulderBattleEye = "--battle-eye";
    public const string cmdBoulderLamb = "lamb";
    public const string cmdBoulderLambClient = "lamb client";
    public const string cmdBoulderLambServer = "lamb server";
    
    public const string LogFolder = "logs";
    
    public static string GetConfigPath(bool testlive)
    {
        var folder = typeof(Config).GetStandardFolder(Environment.SpecialFolder.ApplicationData);
        if(!folder.Exists)
            Directory.CreateDirectory(folder.FullName);
        return Path.Combine(folder.FullName, testlive ? Constants.FileTestLiveConfig : Constants.FileLiveConfig);
    }
    
    public static DirectoryInfo GetLoggingDirectory()
    {
        var folder = typeof(Config).GetStandardFolder(Environment.SpecialFolder.ApplicationData);
        if(!folder.Exists)
            Directory.CreateDirectory(folder.FullName);
        return new DirectoryInfo(Path.Combine(folder.FullName, LogFolder));
    }
}