namespace TrebuchetLib.Services;

public class AppSetup(bool isTestLive, bool catapult)
{
    private bool _hasCatapulted = false;
    
    public bool IsTestLive { get; } = isTestLive;
    
    public bool Catapult { get; } = catapult;

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

    public bool CatapultTrigger()
    {
        if (!_hasCatapulted)
        {
            _hasCatapulted = true;
            return true;
        }
        return false;
    }
}