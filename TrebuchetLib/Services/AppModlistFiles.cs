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

    private bool ResolveMod(uint appId, ref string mod)
    {
        string file = mod;
        if (long.TryParse(mod, out _))
            file = Path.Combine(setup.GetWorkshopFolder(), appId.ToString(), mod, "none");

        string? folder = Path.GetDirectoryName(file);
        if (folder == null)
            return false;

        if (!long.TryParse(Path.GetFileName(folder), out _))
            return File.Exists(file);

        if (!Directory.Exists(folder))
            return false;

        string[] files = Directory.GetFiles(folder, "*.pak", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
            return false;

        mod = files[0];
        return true;
    }
    
    public bool ResolveMod(ref string mod)
    {
        try
        {
            if (ResolveMod(Constants.AppIDLiveClient, ref mod))
                return true;
            if (ResolveMod(Constants.AppIDTestLiveClient, ref mod))
                return true;
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    public IEnumerable<string> ResolveMods(IEnumerable<string> modlist, bool throwIfFailed = true)
    {
        foreach (string mod in modlist)
        {
            string path = mod;
            if (!ResolveMod(ref path))
                if (throwIfFailed)
                    throw new TrebException($"Could not resolve mod {path}.");
                else yield return mod;
            else
                yield return path;
        }
    }
}