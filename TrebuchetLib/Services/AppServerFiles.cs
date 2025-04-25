namespace TrebuchetLib.Services;

public class AppServerFiles(AppSetup appSetup) : IAppServerFiles
{
    private readonly Dictionary<ServerProfileRef, ServerProfile> _cache = [];

    public ServerProfileRef Ref(string name)
    {
        return new ServerProfileRef(name, this);
    }
    
    public ServerProfile Create(ServerProfileRef name)
    {
        if (_cache.TryGetValue(name, out var profile))
        {
            profile.SaveFile();
            return profile;
        }
        var file = ServerProfile.CreateProfile(GetPath(name));
        file.SaveFile();
        _cache[name] = file;
        return file;
    }

    public ServerProfile Get(ServerProfileRef name)
    {
        if (_cache.TryGetValue(name, out var profile))
            return profile;
        
        ServerProfile.RepairMissingProfileFile(GetPath(name));
        var file = ServerProfile.LoadProfile(GetPath(name));
        _cache[name] = file;
        return file;
    }

    public bool Exists(ServerProfileRef name)
    {
        ServerProfile.RepairMissingProfileFile(GetPath(name));
        return File.Exists(GetPath(name));
    }
    
    public bool Exists(string name)
    {
        ServerProfile.RepairMissingProfileFile(GetPath(name));
        return File.Exists(GetPath(name));
    }

    public void Delete(ServerProfileRef name)
    {
        var profile = Get(name);
        _cache.Remove(name);
        profile.DeleteFolder();
    }

    public async Task<ServerProfile> Duplicate(ServerProfileRef name, ServerProfileRef destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        await profile.CopyFolderTo(GetPath(destination));
        var copy = Get(destination);
        return copy;
    }

    public Task<ServerProfile> Rename(ServerProfileRef name, ServerProfileRef destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        profile.MoveFolderTo(GetPath(destination));
        _cache.Remove(name);
        return Task.FromResult(Get(destination));
    }

    public Task<long> GetSize(ServerProfileRef name)
    {
        var dir = Path.GetDirectoryName(GetPath(name));
        if (dir is null) return Task.FromResult(0L);
        return Task.Run(() => Tools.DirectorySize(dir));
    }

    public string GetBaseFolder()
    {
        return Path.Combine(
            appSetup.GetDataDirectory().FullName,
            appSetup.VersionFolder,
            Constants.FolderServerProfiles);
    }

    public string GetPath(ServerProfileRef reference) => GetPath(reference.Name);
    private string GetPath(string name)
    {
        return Path.Combine(
            GetBaseFolder(), 
            name, 
            Constants.FileProfileConfig);
    }

    /// <summary>
    /// List all the server profiles in the installation folder.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ServerProfileRef> GetList()
    {
        if (!Directory.Exists(GetBaseFolder()))
            yield break;

        string[] profiles = Directory.GetDirectories(GetBaseFolder(), "*");
        foreach (string p in profiles)
            yield return Ref(Path.GetFileName(p));
    }

    public ServerProfileRef GetDefault()
    {
        var profile = Ref(Tools.GetFirstDirectoryName(GetBaseFolder(), "*"));
        if (!string.IsNullOrEmpty(profile.Name)) 
            return profile;

        profile = Ref("Default");
        if (!File.Exists(GetPath(profile)))
            ServerProfile.CreateProfile(GetPath(profile)).SaveFile();
        return profile;
    }

    public string GetGameLogs(ServerProfileRef name)
    {
        return Path.Combine(
            GetBaseFolder(),
            name.Name,
            Constants.FolderGameSaveLog,
            Constants.FileGameLogFile
        );
    }
}