namespace TrebuchetLib.Services;

public class AppSetup
{
    public AppSetup(Config config, bool isTestLive, bool catapult)
    {
        IsTestLive = isTestLive;
        Catapult = catapult;
        Config = config;
    }

    public Config Config { get; }
    
    public bool IsTestLive { get; }
    
    public bool Catapult { get; }

    public uint ServerAppId => IsTestLive ? Constants.AppIDTestLiveServer : Constants.AppIDLiveServer;
    
    public string VersionFolder => IsTestLive ? Constants.FolderTestLive : Constants.FolderLive;

}