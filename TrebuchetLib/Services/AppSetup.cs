using System.Diagnostics.CodeAnalysis;
using tot_lib;

namespace TrebuchetLib.Services;

public class AppSetup
{
    public AppSetup(Config config, bool isTestLive, bool catapult, bool experiment)
    {
        IsTestLive = isTestLive;
        Catapult = catapult;
        Config = config;
        Experiment = experiment;
    }

    public Config Config { get; }
    
    public bool IsTestLive { get; }
    
    public bool Catapult { get; }
    
    public bool Experiment { get; }

    public uint ServerAppId => IsTestLive ? Constants.AppIDTestLiveServer : Constants.AppIDLiveServer;
    
    public string VersionFolder => IsTestLive ? Constants.FolderTestLive : Constants.FolderLive;
    
    public DirectoryInfo GetDataDirectory()
    {
        return typeof(Config).GetStandardFolder(Environment.SpecialFolder.MyDocuments);
    }
    
    public DirectoryInfo GetCommonAppDataDirectory()
    {
        if (TryGetCustomDirectory(Config.DataDirectory, out var dir))
            return dir;
        return GetCommonAppDataDirectoryDefault();
    }

    public static DirectoryInfo GetAppConfigDirectory()
    {
        return typeof(Config).GetStandardFolder(Environment.SpecialFolder.ApplicationData);
    }

    private bool TryGetCustomDirectory(string dirPath, [NotNullWhen(true)]out DirectoryInfo? dir)
    {
        dir = null;
        if (!AppFiles.IsDirectoryValidForData(dirPath)) return false;
        dir = new DirectoryInfo(dirPath);
        return true;
    }

    public static DirectoryInfo GetCommonAppDataDirectoryDefault()
    {
        return typeof(Config).GetStandardFolder(Environment.SpecialFolder.CommonApplicationData);
    }
}