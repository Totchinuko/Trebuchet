namespace TrebuchetLib.Services;

public class AppFiles(AppClientFiles clientFiles, AppServerFiles serverFiles, AppModlistFiles modListFiles)
{
    public AppClientFiles Client { get; } = clientFiles;
    public AppServerFiles Server { get; } = serverFiles;
    public AppModlistFiles Mods { get; } = modListFiles;
}