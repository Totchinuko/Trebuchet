using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using TrebuchetLib.Processes;
using TrebuchetUtils;

namespace TrebuchetLib.Services;

public class AppServerFiles(AppSetup appSetup)
{

    /// <summary>
    /// Get the folder of a server profile.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string GetFolder(string name)
    {
        return Path.Combine(AppFiles.GetDataFolder(),
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
            Tools.GetCommonAppData().FullName, 
            appSetup.VersionFolder, 
            Constants.FolderServerInstances);
    }

    public string GetBaseInstancePath(bool testlive)
    {
        return Path.Combine(
            Tools.GetCommonAppData().FullName, 
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
            Tools.GetCommonAppData().FullName, 
            appSetup.VersionFolder, 
            Constants.FolderServerInstances,
            string.Format(Constants.FolderInstancePattern, instance), 
            Constants.FileServerProxyBin);
    }

    public string GetBaseFolder()
    {
        return Path.Combine(
            AppFiles.GetDataFolder(),
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
            AppFiles.GetDataFolder(), 
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

        profileName = Tools.GetFirstDirectoryName(Path.Combine(AppFiles.GetDataFolder(), appSetup.VersionFolder, Constants.FolderServerProfiles), "*");
        if (!string.IsNullOrEmpty(profileName)) 
            return profileName;

        profileName = "Default";
        if (!File.Exists(GetPath(profileName)))
            ServerProfile.CreateFile(GetPath(profileName)).SaveFile();
        return profileName;
    }

    /// <summary>
    /// Try to load a profile, if it fails, it will return false.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="profile"></param>
    /// <returns></returns>
    public bool TryLoadProfile(string name, [NotNullWhen(true)] out ServerProfile? profile)
    {
        profile = null;
        string profilePath = GetPath(name);
        if (!File.Exists(profilePath)) return false;
        try
        {
            profile = ServerProfile.LoadProfile(profilePath);
            return true;
        }
        catch { return false; }
    }

    public bool TryGetInstanceIndexFromPath(string path, out int instance)
    {
        instance = -1;
        for (int i = 0; i < appSetup.Config.ServerInstanceCount; i++)
        {
            var instancePath = Path.GetFullPath(GetIntanceBinary(i));
            if (string.Equals(instancePath, path, StringComparison.Ordinal))
            {
                instance = i;
                return true;
            }
        }
        return false;
    }
}