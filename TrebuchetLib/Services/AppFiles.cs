using tot_lib;
using tot_lib.OsSpecific;

namespace TrebuchetLib.Services;

public class AppFiles(AppSetup setup, IOsPlatformSpecific osSpecific)
{
    public IAppClientFiles Client { get; } = new AppClientFiles(setup);
    public IAppServerFiles Server { get; } = new AppServerFiles(setup);
    public IAppModListFiles Mods { get; } = new AppModlistFiles(setup);
    public IAppSyncFiles Sync { get; } = new AppSyncFiles(setup);

    public bool SetupFolders()
    {
        Tools.CreateDir(setup.GetServerInstancePath());
        Tools.CreateDir(Client.GetBaseFolder());
        Tools.CreateDir(Server.GetBaseFolder());
        Tools.CreateDir(Mods.GetBaseFolder());
        Tools.CreateDir(Sync.GetBaseFolder());
        Tools.CreateDir(setup.GetWorkshopFolder());
        Tools.CreateDir(setup.GetEmptyJunction());
        if(!osSpecific.IsSymbolicLink(setup.GetPrimaryJunction()))
            osSpecific.MakeSymbolicLink(setup.GetPrimaryJunction(), setup.GetEmptyJunction());

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