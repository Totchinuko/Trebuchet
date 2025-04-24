using System.Diagnostics.CodeAnalysis;

namespace TrebuchetLib.Services;

public class AppClientFiles(AppSetup appSetup) : IAppClientFiles
{
    private readonly Dictionary<string, ClientProfile> _cache = [];
    public ClientProfile Create(string name)
    {
        if (_cache.TryGetValue(name, out var profile))
        {
            profile.SaveFile();
            return profile;
        }
        var file = ClientProfile.CreateProfile(GetPath(name));
        file.SaveFile();
        _cache[name] = file;
        return file;
    }

    public ClientProfile Get(string name)
    {
        if (_cache.TryGetValue(name, out var profile))
            return profile;
        
        ClientProfile.RepairMissingProfileFile(GetPath(name));
        var file = ClientProfile.LoadProfile(GetPath(name));
        _cache[name] = file;
        return file;
    }

    public bool Exists(string name)
    {
        ClientProfile.RepairMissingProfileFile(GetPath(name));
        return File.Exists(GetPath(name));
    }

    public void Delete(string name)
    {
        var profile = Get(name);
        _cache.Remove(name);
        profile.DeleteFolder();
    }

    public async Task<ClientProfile> Duplicate(string name, string destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        await profile.CopyFolderTo(GetPath(destination));
        var copy = Get(destination);
        return copy;
    }

    public Task<ClientProfile> Rename(string name, string destination)
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
            appSetup.GetDataDirectory().FullName,
            appSetup.VersionFolder,
            Constants.FolderClientProfiles);
    }

    public string GetPath(string name)
    {
        return Path.Combine(
            GetBaseFolder(), 
            name, 
            Constants.FileProfileConfig);
    }

    public IEnumerable<string> GetList()
    {
        if (!Directory.Exists(GetBaseFolder()))
            yield break;

        string[] profiles = Directory.GetDirectories(GetBaseFolder(), "*");
        foreach (string p in profiles)
            yield return Path.GetFileName(p);
    }

    public Task<long> GetSize(string name)
    {
        var dir = Path.GetDirectoryName(GetPath(name));
        if (dir is null) return Task.FromResult(0L);
        return Task.Run(() => Tools.DirectorySize(dir));
    }
    
    public string GetDefault()
    {
        var name = Tools.GetFirstDirectoryName(GetBaseFolder(), "*");
        if (!string.IsNullOrEmpty(name)) 
            return name;

        name = "Default";
        if (!File.Exists(GetPath(name)))
            ClientProfile.CreateProfile(GetPath(name)).SaveFile();
        return name;
    }
    
    public string GetGameLogs(string name)
    {
        return Path.Combine(
            GetBaseFolder(),
            name,
            Constants.FolderGameSaveLog,
            Constants.FileGameLogFile
        );
    }
}