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

}