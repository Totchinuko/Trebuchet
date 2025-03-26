using System;
using System.IO;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.Services;

public class AppFiles(AppSetup setup)
{
    public string GetPath()
    {
        string? ConfigPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        if (string.IsNullOrEmpty(ConfigPath))
            throw new Exception("Path to assembly is invalid.");
        ConfigPath = Path.Combine(ConfigPath, $"{(setup.IsTestLive ? Constants.FolderTestLive : Constants.FolderLive)}.UIConfig.json");
        return ConfigPath;
    }
}