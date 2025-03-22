namespace TrebuchetLib.Services;

public class AppSetup(bool isTestLive, bool catapult)
{
    public bool IsTestLive { get; } = isTestLive;
    
    public bool Catapult { get; } = catapult;

    public uint ServerAppId => IsTestLive ? Constants.AppIDTestLiveServer : Constants.AppIDLiveServer;
    
    public string VersionFolder => IsTestLive ? Constants.FolderTestLive : Constants.FolderLive;
}