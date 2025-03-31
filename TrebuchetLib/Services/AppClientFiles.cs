using System.Diagnostics.CodeAnalysis;

namespace TrebuchetLib.Services;

public class AppClientFiles(AppSetup appSetup)
{
    public string GetFolder(string name)
    {
        return Path.Combine(AppFiles.GetDataFolder(), appSetup.VersionFolder, Constants.FolderClientProfiles, name);
    }

    public string GetBaseFolder()
    {
        return Path.Combine(
            AppFiles.GetDataFolder(),
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
    
    public IEnumerable<string> ListProfiles()
    {
        string folder = Path.Combine(AppFiles.GetDataFolder(), appSetup.VersionFolder, Constants.FolderClientProfiles);
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

        profileName = Tools.GetFirstDirectoryName(Path.Combine(AppFiles.GetDataFolder(), appSetup.VersionFolder, Constants.FolderClientProfiles), "*");
        if (!string.IsNullOrEmpty(profileName)) 
            return profileName;

        profileName = "Default";
        if (!File.Exists(GetPath(profileName)))
            ClientProfile.CreateProfile(GetPath(profileName)).SaveFile();
        return profileName;
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
}