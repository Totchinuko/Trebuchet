using System.Web;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;

namespace TrebuchetLib.Services;

public class AppSyncFiles(AppSetup setup) : 
    IAppSyncFiles
{
    private readonly Dictionary<SyncProfileRef, SyncProfile> _cache = [];

    public SyncProfileRef Ref(string name)
    {
        return new SyncProfileRef(name, this);
    }
    
    public SyncProfile Create(SyncProfileRef name)
    {
        if (_cache.TryGetValue(name, out var profile))
        {
            profile.SaveFile();
            return profile;
        }
        var file = SyncProfile.CreateProfile(GetPath(name));
        file.SaveFile();
        _cache[name] = file;
        return file;
    }

    public SyncProfile Get(SyncProfileRef name)
    {
        if (_cache.TryGetValue(name, out var profile))
            return profile;
        var file = SyncProfile.LoadProfile(GetPath(name));
        _cache[name] = file;
        return file;
    }

    public bool Exists(SyncProfileRef name)
    {
        return File.Exists(GetPath(name));
    }
    
    public bool Exists(string name)
    {
        return File.Exists(GetPath(name));
    }

    public void Delete(SyncProfileRef name)
    {
        var profile = Get(name);
        _cache.Remove(name);
        profile.DeleteFile();
    }
    
    public SyncProfileRef GetDefault()
    {
        var profileName = Ref(Tools.GetFirstFileName(GetBaseFolder(), "*.json"));
        if (!string.IsNullOrEmpty(profileName.Name)) 
            return profileName;

        profileName = Ref("Default");
        if (!File.Exists(GetPath(profileName)))
            SyncProfile.CreateProfile(GetPath(profileName)).SaveFile();
        return profileName;
    }

    public Task<SyncProfile> Duplicate(SyncProfileRef name, SyncProfileRef destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        profile.CopyFileTo(GetPath(destination));
        return Task.FromResult(Get(destination));
    }

    public Task<SyncProfile> Rename(SyncProfileRef name, SyncProfileRef destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        profile.MoveFolderTo(GetPath(destination));
        _cache.Remove(name);
        return Task.FromResult(Get(destination));
    }

    public string GetBaseFolder()
    {
        return Path.Combine(
            setup.GetDataDirectory().FullName, 
            setup.VersionFolder, 
            Constants.FolderSyncProfiles);
    }

    public string GetPath(SyncProfileRef reference) => GetPath(reference.Name);
    private string GetPath(string modlistName)
    {
        return Path.Combine(
            GetBaseFolder(),
            modlistName + ".json");
    }

    public IEnumerable<SyncProfileRef> GetList()
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
    
    public IEnumerable<string> GetResolvedModlist(IEnumerable<string> mods, bool throwIfFailed = true)
    {
        foreach (string mod in mods)
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

    public Task Export(SyncProfileRef name, FileInfo file)
    {
        var path = GetPath(name);
        File.Copy(path, file.FullName, true);
        return Task.CompletedTask;
    }

    public async Task<SyncProfile> Import(FileInfo import, SyncProfileRef name)
    {
        var json = await File.ReadAllTextAsync(import.FullName);
        return await Import(json, name);
    }

    public Task<SyncProfile> Import(string json, SyncProfileRef name)
    {
        var path = GetPath(name);
        var profile = SyncProfile.ImportFile(json, path);
        _cache[name] = profile;
        return Task.FromResult(profile);
    }

    public async Task Sync(SyncProfileRef name)
    {
        var profile = Get(name);
        if (string.IsNullOrWhiteSpace(profile.SyncURL))
            throw new Exception("Url is invalid");
        
        UriBuilder builder = new UriBuilder(profile.SyncURL);

        if (SteamWorks.SteamCommunityHost == builder.Host)
        {
            profile.Modlist = await SyncSteamCollection(builder);
            profile.SaveFile();
        }
        else
            await SyncJson(builder, name);
    }
    
    private async Task SyncJson(UriBuilder builder, SyncProfileRef name)
    {
        var result = await Tools.DownloadModList(builder.ToString(), CancellationToken.None);
        await Import(result, name);
    }

    private async Task<List<string>> SyncSteamCollection(UriBuilder builder)
    {
        var query = HttpUtility.ParseQueryString(builder.Query);
        var id = query.Get(@"id");
        if (id == null || !ulong.TryParse(id, out var collectionId))
            throw new Exception("Steam Collection URL is invalid");

        var result = await SteamRemoteStorage.GetCollectionDetails(
            new GetCollectionDetailsQuery(collectionId), CancellationToken.None);

        return result.CollectionDetails
            .First()
            .Children.Select(x => x.PublishedFileId).ToList();
    }
}