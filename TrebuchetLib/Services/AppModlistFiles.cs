using SteamKit2.WebUI.Internal;

namespace TrebuchetLib.Services;

public class AppModlistFiles(AppSetup setup) : IAppModListFiles
{
    private readonly Dictionary<ModListProfileRef, ModListProfile> _cache = [];

    public bool UseSubFolders => false;
    public Dictionary<ModListProfileRef, ModListProfile> Cache => _cache;
    
    public ModListProfileRef Ref(string name)
    {
        return new ModListProfileRef(name, this);
    }

    public string GetBaseFolder()
    {
        return Path.Combine(
            setup.GetDataDirectory().FullName, 
            setup.VersionFolder, 
            Constants.FolderModlistProfiles);
    }
}