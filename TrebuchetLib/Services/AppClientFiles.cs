using System.Diagnostics.CodeAnalysis;

namespace TrebuchetLib.Services;

public class AppClientFiles(Config config, AppSetup appSetup)
{
    public string GetFolder(string name)
    {
        return Path.Combine(config.ResolvedInstallPath(), appSetup.VersionFolder, Constants.FolderClientProfiles, name);
    }

    public string GetPath(string name)
    {
        return Path.Combine(config.ResolvedInstallPath(), appSetup.VersionFolder, Constants.FolderClientProfiles, name, 
            Constants.FileProfileConfig);
    }
    
    public IEnumerable<string> ListProfiles()
    {
        string folder = Path.Combine(config.ResolvedInstallPath(), appSetup.VersionFolder, Constants.FolderClientProfiles);
        if (!Directory.Exists(folder))
            yield break;

        string[] profiles = Directory.GetDirectories(folder, "*");
        foreach (string p in profiles)
            yield return Path.GetFileName(p);
    }
    
    public void ResolveProfile(ref string profileName)
    {
        if (!string.IsNullOrEmpty(profileName))
        {
            string path = GetPath(profileName);
            if (File.Exists(path)) return;
        }

        profileName = Tools.GetFirstDirectoryName(Path.Combine(config.ResolvedInstallPath(), appSetup.VersionFolder, Constants.FolderClientProfiles), "*");
        if (!string.IsNullOrEmpty(profileName)) return;

        profileName = "Default";
        if (!File.Exists(GetPath(profileName)))
            ClientProfile.CreateProfile(GetPath(profileName)).SaveFile();
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
    
    public bool TryLoadProfile(string name, [NotNullWhen(true)] out ClientProfile? profile)
    {
        profile = null;
        string profilePath = GetPath(name);
        if (!File.Exists(profilePath)) return false;
        try
        {
            profile = ClientProfile.LoadProfile(profilePath);
            return true;
        }
        catch { return false; }
    }

    public string GetGameBinaryPath()
    {
        return Path.Combine(config.ClientPath, Constants.FolderGameBinaries, Constants.FileClientBin);
    }
    public string GetBattleEyeBinaryPath()
    {
        return Path.Combine(config.ClientPath, Constants.FolderGameBinaries, Constants.FileClientBEBin);
    }

    public string GetClientFolder()
    {
        return config.ClientPath;
    }
}