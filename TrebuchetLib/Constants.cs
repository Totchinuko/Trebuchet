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
    public const string RegexSavedFolder = @"ConanSandbox([\\/]+)Saved";
    public const string ServerArgsMaxPlayers = "-MaxPlayers={0}";
    public const string ServerArgsMultiHome = "-MULTIHOME={0}";
    public const string SteamWorkshopURL = "https://steamcommunity.com/sharedfiles/filedetails/?id={0}";
}