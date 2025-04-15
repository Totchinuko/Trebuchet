using tot_lib;

namespace TrebuchetLib.Services;

public class AppFiles(AppClientFiles clientFiles, AppServerFiles serverFiles, AppModlistFiles modListFiles)
{
    public AppClientFiles Client { get; } = clientFiles;
    public AppServerFiles Server { get; } = serverFiles;
    public AppModlistFiles Mods { get; } = modListFiles;
    
    public bool SetupFolders()
    {
        Tools.CreateDir(Server.GetBaseInstancePath());
        Tools.CreateDir(Client.GetBaseFolder());
        Tools.CreateDir(Server.GetBaseFolder());
        Tools.CreateDir(Mods.GetBaseFolder());
        Tools.CreateDir(Mods.GetWorkshopFolder());
        Tools.CreateDir(Client.GetEmptyJunction());
        if(!JunctionPoint.Exists(Client.GetPrimaryJunction()))
            Tools.SetupSymboliclink(Client.GetPrimaryJunction(), Client.GetEmptyJunction());

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