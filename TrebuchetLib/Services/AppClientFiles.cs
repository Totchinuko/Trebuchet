using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace TrebuchetLib.Services;

public class AppClientFiles(AppSetup appSetup)
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
        return File.Exists(GetPath(name));
    }

    public void Delete(string name)
    {
        var profile = Get(name);
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

    public ClientProfile Move(string name, string destination)
    {
        if (Exists(destination)) throw new Exception("Destination profile exists");
        if (!Exists(name)) throw new Exception("Source profile does not exists");
        var profile = Get(name);
        profile.MoveFolderTo(GetPath(destination));
        _cache.Remove(name);
        var moved = Get(destination);
        return moved;
    }
    
    public string GetFolder(string name)
    {
        return Path.Combine(appSetup.GetDataDirectory().FullName, appSetup.VersionFolder, Constants.FolderClientProfiles, name);
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

    public string GetPrimaryJunction()
    {
        return Path.Combine(
            appSetup.GetCommonAppDataDirectory().FullName,
            Constants.GamePrimaryJunction
        );
    }

    public string GetEmptyJunction()
    {
        return Path.Combine(
            appSetup.GetCommonAppDataDirectory().FullName,
            Constants.GameEmptyJunction
        );
    }
    
    public IEnumerable<string> ListProfiles()
    {
        string folder = Path.Combine(appSetup.GetDataDirectory().FullName, appSetup.VersionFolder, Constants.FolderClientProfiles);
        if (!Directory.Exists(folder))
            yield break;

        string[] profiles = Directory.GetDirectories(folder, "*");
        foreach (string p in profiles)
            yield return Path.GetFileName(p);
    }
    
    public string ResolveProfile(string profileName)
    {
        if (!string.IsNullOrEmpty(profileName))
        {
            string path = GetPath(profileName);
            if (File.Exists(path)) 
                return profileName;
        }

        return GetDefaultProfile();
    }

    public string GetDefaultProfile()
    {
        var name = Tools.GetFirstDirectoryName(Path.Combine(appSetup.GetDataDirectory().FullName, appSetup.VersionFolder, Constants.FolderClientProfiles), "*");
        if (!string.IsNullOrEmpty(name)) 
            return name;

        name = "Default";
        if (!File.Exists(GetPath(name)))
            ClientProfile.CreateProfile(GetPath(name)).SaveFile();
        return name;
    }
    
    public bool ProfileExists(string name)
    {
        return File.Exists(GetPath(name));
    }
    
    public string GetUniqueOriginalProfile()
    {
        int i = 1;
        while (ProfileExists("_Original_" + i)) i++;
        return GetPath("_Original_" + i);
    }
    
    public bool TryGet(string name, [NotNullWhen(true)] out ClientProfile? profile)
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

    public string GetGameBinaryPath()
    {
        return Path.Combine(appSetup.Config.ClientPath, Constants.FolderGameBinaries, Constants.FileClientBin);
    }
    public string GetBattleEyeBinaryPath()
    {
        return Path.Combine(appSetup.Config.ClientPath, Constants.FolderGameBinaries, Constants.FileClientBEBin);
    }

    public string GetClientFolder()
    {
        return appSetup.Config.ClientPath;
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