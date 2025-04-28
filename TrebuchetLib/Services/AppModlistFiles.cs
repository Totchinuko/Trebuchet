using SteamKit2.WebUI.Internal;

namespace TrebuchetLib.Services;

public class AppModlistFiles(AppSetup setup) : IAppModListFiles
{
    private readonly Dictionary<ModListProfileRef, ModListProfile> _cache = [];

    public ModListProfileRef Ref(string name)
    {
        return new ModListProfileRef(name, this);
    }

    public ModListProfile Create(ModListProfileRef name)
    {
        if (_cache.TryGetValue(name, out var profile))
        {
            profile.SaveFile();
            return profile;
        }
        var file = ModListProfile.CreateProfile(GetPath(name));
        file.SaveFile();
        _cache[name] = file;
        return file;
    }

    public ModListProfile Get(ModListProfileRef name)
    {
        if (_cache.TryGetValue(name, out var profile))
            return profile;
        var file = ModListProfile.LoadProfile(GetPath(name));
        _cache[name] = file;
        return file;
    }

    public bool Exists(ModListProfileRef name)
    {
        return File.Exists(GetPath(name));
    }

    public bool Exists(string name)
    {
        return File.Exists(GetPath(name));
    }

    public void Delete(ModListProfileRef name)
    {
        var profile = Get(name);
        _cache.Remove(name);
        profile.DeleteFile();
    }
    
    public ModListProfileRef GetDefault()
    {
        var profile = Ref(Tools.GetFirstFileName(GetBaseFolder(), "*.json"));
        if (!string.IsNullOrEmpty(profile.Name))
            return profile;

        profile = Ref("Default");
        if (!File.Exists(GetPath(profile)))
            ModListProfile.CreateProfile(GetPath(profile)).SaveFile();
        return profile;
    }

    public Task<ModListProfile> Duplicate(ModListProfileRef name, ModListProfileRef destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        profile.CopyFileTo(GetPath(destination));
        return Task.FromResult(Get(destination));
    }

    public Task<ModListProfile> Rename(ModListProfileRef name, ModListProfileRef destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        profile.MoveFileTo(GetPath(destination));
        _cache.Remove(name);
        return Task.FromResult(Get(destination));
    }
 
    public string GetBaseFolder()
    {
        return Path.Combine(
            setup.GetDataDirectory().FullName, 
            setup.VersionFolder, 
            Constants.FolderModlistProfiles);
    }

    public string GetPath(ModListProfileRef reference) => GetPath(reference.Name);
    private string GetPath(string modlistName)
    {
        return Path.Combine(
            GetBaseFolder(),
            modlistName + ".json");
    }

    public IEnumerable<ModListProfileRef> GetList()
    {
        if (!Directory.Exists(GetBaseFolder()))
            yield break;

        string[] profiles = Directory.GetFiles(GetBaseFolder(), "*.json");
        foreach (string p in profiles)
            yield return Ref(Path.GetFileNameWithoutExtension(p));
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

    public Task Export(ModListProfileRef name, FileInfo file)
    {
        var path = GetPath(name);
        File.Copy(path, file.FullName, true);
        return Task.CompletedTask;
    }

    public async Task<ModListProfile> Import(FileInfo import, ModListProfileRef name)
    {
        var json = await File.ReadAllTextAsync(import.FullName);
        return await Import(json, name);
    }

    public Task<ModListProfile> Import(string json, ModListProfileRef name)
    {
        var path = GetPath(name);
        var profile = ModListProfile.ImportFile(json, path);
        _cache[name] = profile;
        return Task.FromResult(profile);
    }
}