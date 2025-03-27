namespace TrebuchetLib.Services;

public class AppSetup
{
    public AppSetup(bool isTestLive, bool catapult)
    {
        IsTestLive = isTestLive;
        Catapult = catapult;
        Config = Config.LoadConfig(GetConfigPath());
    }

    public Config Config { get; }
    
    public bool IsTestLive { get; }
    
    public bool Catapult { get; }

    public uint ServerAppId => IsTestLive ? Constants.AppIDTestLiveServer : Constants.AppIDLiveServer;
    
    public string VersionFolder => IsTestLive ? Constants.FolderTestLive : Constants.FolderLive;
    
    public string GetConfigPath()
    {
        string? configPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        if (string.IsNullOrEmpty(configPath))
            throw new TrebException("Path to assembly is invalid.");
        configPath = Path.Combine(configPath,
            $"{(IsTestLive ? Constants.FolderTestLive : Constants.FolderLive)}.{Constants.FileConfig}");
        return configPath;
    }
}