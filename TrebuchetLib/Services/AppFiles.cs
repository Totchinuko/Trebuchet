namespace TrebuchetLib.Services;

public class AppFiles(AppSetup setup, AppClientFiles clientFiles, AppServerFiles serverFiles, AppModlistFiles modListFiles)
{
    public AppClientFiles Client { get; } = clientFiles;
    public AppServerFiles Server { get; } = serverFiles;
    public AppModlistFiles Mods { get; } = modListFiles;
    
    public string GetConfigPath()
    {
        string? ConfigPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        if (string.IsNullOrEmpty(ConfigPath))
            throw new Exception("Path to assembly is invalid.");
        ConfigPath = Path.Combine(ConfigPath, $"{(setup.IsTestLive ? Constants.FolderTestLive : Constants.FolderLive)}.UIConfig.json");
        return ConfigPath;
    }
}