using TrebuchetUtils;

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
    
    public static DirectoryInfo GetDataDirectory()
    {
        return typeof(Config).GetStandardFolder(Environment.SpecialFolder.MyDocuments);
    }
    
    public static DirectoryInfo GetCommonAppDataDirectory()
    {
        return typeof(Tools).GetStandardFolder(Environment.SpecialFolder.CommonApplicationData);
    }
}