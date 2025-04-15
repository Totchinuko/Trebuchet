using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using TrebuchetLib.Processes;

namespace TrebuchetLib.Services;

public class AppServerFiles(AppSetup appSetup)
{
    
    private Dictionary<string, ServerProfile> _cache = [];
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

    public ServerProfile Move(string name, string destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        profile.MoveFolderTo(GetPath(destination));
        _cache.Remove(name);
        var moved = Get(destination);
        return moved;
    }

    /// <summary>
    /// Get the folder of a server profile.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string GetFolder(string name)
    {
        return Path.Combine(AppFiles.GetDataDirectory().FullName,
            appSetup.VersionFolder, Constants.FolderServerProfiles, name);
    }

    /// <summary>
    /// Get the path of a server instance.
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    public string GetInstancePath(int instance)
    {
        return Path.Combine(
            GetBaseInstancePath(),
            string.Format(Constants.FolderInstancePattern, instance));
    }

    public string GetBaseInstancePath(DirectoryInfo baseFolder)
    {
        return Path.Combine(
            baseFolder.FullName, 
            appSetup.VersionFolder, 
            Constants.FolderServerInstances);
    }
    
    public string GetBaseInstancePath()
    {
        return Path.Combine(
            AppFiles.GetCommonAppDataDirectory().FullName, 
            appSetup.VersionFolder, 
            Constants.FolderServerInstances);
    }

    public string GetBaseInstancePath(bool testlive)
    {
        return Path.Combine(
            AppFiles.GetCommonAppDataDirectory().FullName, 
            testlive ? Constants.FolderTestLive : Constants.FolderLive, 
            Constants.FolderServerInstances);
    }

    /// <summary>
    /// Get the executable of a server instance.
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    public string GetIntanceBinary(int instance)
    {
        return Path.Combine(
            AppFiles.GetCommonAppDataDirectory().FullName, 
            appSetup.VersionFolder, 
            Constants.FolderServerInstances,
            string.Format(Constants.FolderInstancePattern, instance), 
            Constants.FileServerProxyBin);
    }

    public string GetInstanceInternalBinary(int instance)
    {
        return Path.Combine(
            AppFiles.GetCommonAppDataDirectory().FullName, 
            appSetup.VersionFolder, 
            Constants.FolderServerInstances,
            string.Format(Constants.FolderInstancePattern, instance), 
            Constants.FolderGameBinaries,
            Constants.FileServerBin);
    }

    public string GetBaseFolder()
    {
        return Path.Combine(
            AppFiles.GetDataDirectory().FullName,
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
    public IEnumerable<string> ListProfiles()
    {
        string folder = Path.Combine(
            AppFiles.GetDataDirectory().FullName, 
            appSetup.VersionFolder, 
            Constants.FolderServerProfiles);
        if (!Directory.Exists(folder))
            yield break;

        string[] profiles = Directory.GetDirectories(folder, "*");
        foreach (string p in profiles)
            yield return Path.GetFileName(p);
    }

    /// <summary>
    /// Resolve a server profile name, if it is not valid, it will be set to the first profile found, if no profile is found, it will be set to "Default" and a new profile will be created.
    /// </summary>
    /// <param name="profileName"></param>
    public string ResolveProfile(string profileName)
    {
        if (!string.IsNullOrEmpty(profileName))
        {
            string path = GetPath(profileName);
            if (File.Exists(path)) 
                return profileName;
        }

        profileName = Tools.GetFirstDirectoryName(Path.Combine(AppFiles.GetDataDirectory().FullName, appSetup.VersionFolder, Constants.FolderServerProfiles), "*");
        if (!string.IsNullOrEmpty(profileName)) 
            return profileName;

        profileName = "Default";
        if (!File.Exists(GetPath(profileName)))
            ServerProfile.CreateProfile(GetPath(profileName)).SaveFile();
        return profileName;
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

    public bool TryGetInstanceIndexFromPath(string path, out int instance)
    {
        instance = -1;
        for (int i = 0; i < appSetup.Config.ServerInstanceCount; i++)
        {
            var instancePath = Path.GetFullPath(GetInstanceInternalBinary(i));
            if (string.Equals(instancePath, path, StringComparison.Ordinal))
            {
                instance = i;
                return true;
            }
        }
        return false;
    }
}