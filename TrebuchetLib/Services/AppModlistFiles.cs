using SteamKit2.WebUI.Internal;

namespace TrebuchetLib.Services;

public class AppModlistFiles(AppSetup setup) : IAppModListFiles
{
    private readonly Dictionary<string, ModListProfile> _cache = [];
    public ModListProfile Create(string name)
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

    public ModListProfile Get(string name)
    {
        if (_cache.TryGetValue(name, out var profile))
            return profile;
        var file = ModListProfile.LoadProfile(GetPath(name));
        _cache[name] = file;
        return file;
    }

    public bool Exists(string name)
    {
        return File.Exists(GetPath(name));
    }

    public void Delete(string name)
    {
        var profile = Get(name);
        profile.DeleteFile();
    }
    
    public string GetDefault()
    {
        var profileName = Tools.GetFirstFileName(Path.Combine(setup.GetDataDirectory().FullName, setup.VersionFolder, Constants.FolderModlistProfiles), "*.json");
        if (!string.IsNullOrEmpty(profileName)) 
            return profileName;

        profileName = "Default";
        if (!File.Exists(GetPath(profileName)))
            ModListProfile.CreateProfile(GetPath(profileName)).SaveFile();
        return profileName;
    }

    public Task<ModListProfile> Duplicate(string name, string destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        profile.CopyFileTo(GetPath(destination));
        return Task.FromResult(Get(destination));
    }

    public Task<ModListProfile> Rename(string name, string destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        profile.MoveFolderTo(GetPath(destination));
        _cache.Remove(name);
        return Task.FromResult(Get(destination));
    }

    public IEnumerable<ulong> CollectAllMods(IEnumerable<string> modlists)
    {
        foreach (var i in modlists.Distinct())
            if (this.TryGet(i, out ModListProfile? profile))
                foreach (var m in profile.Modlist)
                    if (TryParseModId(m, out ulong id))
                        yield return id;
    }

    public IEnumerable<ulong> CollectAllMods(string modlist)
    {
        if (this.TryGet(modlist, out ModListProfile? profile))
            foreach (var m in profile.Modlist)
                if (TryParseModId(m, out ulong id))
                    yield return id;
    }

    public IEnumerable<ulong> GetModIdList(IEnumerable<string> modlist)
    {
        foreach (var mod in modlist)
            if (TryParseModId(mod, out var id))
                yield return id;
    }

    public string GetBaseFolder()
    {
        return Path.Combine(
            setup.GetDataDirectory().FullName, 
            setup.VersionFolder, 
            Constants.FolderModlistProfiles);
    }

    public string GetPath(string modlistName)
    {
        return Path.Combine(
            GetBaseFolder(),
            modlistName + ".json");
    }

    public IEnumerable<string> GetList()
    {
        string folder = Path.Combine(setup.GetDataDirectory().FullName, setup.VersionFolder, Constants.FolderModlistProfiles);
        if (!Directory.Exists(folder))
            yield break;

        string[] profiles = Directory.GetFiles(folder, "*.json");
        foreach (string p in profiles)
            yield return Path.GetFileNameWithoutExtension(p);
    }

    public IEnumerable<string> ParseModList(IEnumerable<string> modlist)
    {
        foreach (var mod in modlist)
            if (TryParseModId(mod, out ulong id))
                yield return id.ToString();
            else
                yield return mod;
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
    
    public IEnumerable<string> GetResolvedModlist(IEnumerable<string> modlist, bool throwIfFailed = true)
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

    public bool TryParseDirectory2ModId(string path, out ulong id)
    {
        id = 0;
        if (ulong.TryParse(Path.GetFileName(path), out id))
            return true;

        string? parent = Path.GetDirectoryName(path);
        if (parent != null && ulong.TryParse(Path.GetFileName(parent), out id))
            return true;

        return false;
    }

    public bool TryParseFile2ModId(string path, out ulong id)
    {
        id = 0;
        string? folder = Path.GetDirectoryName(path);
        if (folder == null)
            return false;
        if (ulong.TryParse(Path.GetFileName(folder), out id))
            return true;

        return false;
    }

    public bool TryParseModId(string path, out ulong id)
    {
        id = 0;
        if (ulong.TryParse(path, out id))
            return true;

        if (Path.GetExtension(path) == ".pak")
            return TryParseFile2ModId(path, out id);
        else
            return TryParseDirectory2ModId(path, out id);
    }

    public Task Export(string name, FileInfo file)
    {
        var path = GetPath(name);
        File.Copy(path, file.FullName, true);
        return Task.CompletedTask;
    }

    public async Task<ModListProfile> Import(FileInfo import, string name)
    {
        var json = await File.ReadAllTextAsync(import.FullName);
        return await Import(json, name);
    }

    public Task<ModListProfile> Import(string json, string name)
    {
        var path = GetPath(name);
        var profile = ModListProfile.ImportFile(json, path);
        _cache[name] = profile;
        return Task.FromResult(profile);
    }
}