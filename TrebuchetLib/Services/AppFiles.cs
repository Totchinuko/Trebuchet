using tot_lib;

namespace TrebuchetLib.Services;

public class AppFiles(AppSetup setup, AppClientFiles clientFiles, AppServerFiles serverFiles, AppModlistFiles modListFiles, AppSyncFiles syncFiles)
{
    public IAppClientFiles Client { get; } = clientFiles;
    public IAppServerFiles Server { get; } = serverFiles;
    public IAppModListFiles Mods { get; } = modListFiles;
    public IAppSyncFiles Sync { get; } = syncFiles;
    
    public bool SetupFolders()
    {
        Tools.CreateDir(setup.GetServerInstancePath());
        Tools.CreateDir(Client.GetBaseFolder());
        Tools.CreateDir(Server.GetBaseFolder());
        Tools.CreateDir(Mods.GetBaseFolder());
        Tools.CreateDir(Sync.GetBaseFolder());
        Tools.CreateDir(setup.GetWorkshopFolder());
        Tools.CreateDir(setup.GetEmptyJunction());
        if(!JunctionPoint.Exists(setup.GetPrimaryJunction()))
            Tools.SetupSymboliclink(setup.GetPrimaryJunction(), setup.GetEmptyJunction());

        return true;
    }

    public static bool IsDirectoryValidForData(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (!Directory.Exists(path)) return false;
            if (!Utils.IsDirectoryWritable(path)) return false;
            return true;
        }
        catch
        {
            return false;
        }
    }
}