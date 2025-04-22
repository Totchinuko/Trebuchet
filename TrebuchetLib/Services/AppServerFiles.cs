using System.Diagnostics.CodeAnalysis;

namespace TrebuchetLib.Services;

public class AppServerFiles(AppSetup appSetup) : IAppServerFiles
{
    private readonly Dictionary<string, ServerProfile> _cache = [];
    public ServerProfile Create(string name)
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

    public ServerProfile Get(string name)
    {
        if (_cache.TryGetValue(name, out var profile))
            return profile;
        
        ServerProfile.RepairMissingProfileFile(GetPath(name));
        var file = ServerProfile.LoadProfile(GetPath(name));
        _cache[name] = file;
        return file;
    }

    public bool Exists(string name)
    {
        ServerProfile.RepairMissingProfileFile(GetPath(name));
        return File.Exists(GetPath(name));
    }

    public void Delete(string name)
    {
        var profile = Get(name);
        profile.DeleteFolder();
    }

    public async Task<ServerProfile> Duplicate(string name, string destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        await profile.CopyFolderTo(GetPath(destination));
        var copy = Get(destination);
        return copy;
    }

    public Task<ServerProfile> Rename(string name, string destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        profile.MoveFolderTo(GetPath(destination));
        _cache.Remove(name);
        return Task.FromResult(Get(destination));
    }

    public Task<long> GetSize(string name)
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

    /// <summary>
    /// Get the path of the json file of a server profile.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string GetPath(string name)
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
    public IEnumerable<string> GetList()
    {
        string folder = Path.Combine(
            appSetup.GetDataDirectory().FullName, 
            appSetup.VersionFolder, 
            Constants.FolderServerProfiles);
        if (!Directory.Exists(folder))
            yield break;

        string[] profiles = Directory.GetDirectories(folder, "*");
        foreach (string p in profiles)
            yield return Path.GetFileName(p);
    }

    /// <summary>
    /// Try to load a profile, if it fails, it will return false.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="profile"></param>
    /// <returns></returns>
    public bool TryGet(string name, [NotNullWhen(true)] out ServerProfile? profile)
    {
        profile = null;
        if (!Exists(name)) return false;
        try
        {
            profile = Get(name);
            return true;
        }
        catch { return false; }
    }
    
    public string GetDefault()
    {
        var name = Tools.GetFirstDirectoryName(Path.Combine(appSetup.GetDataDirectory().FullName, appSetup.VersionFolder, Constants.FolderServerProfiles), "*");
        if (!string.IsNullOrEmpty(name)) 
            return name;

        name = "Default";
        if (!File.Exists(GetPath(name)))
            ServerProfile.CreateProfile(GetPath(name)).SaveFile();
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