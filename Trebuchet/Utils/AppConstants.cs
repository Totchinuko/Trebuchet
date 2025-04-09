using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TrebuchetUtils;
using TrebuchetLib;

namespace Trebuchet.Utils;

public static class AppConstants
{
    public const string ConfigFileName = "settings.ui.json";
    public const string LogFolder = "logs";
    public const string LogFileName = "app.log";

    public static string GetLoggingPath()
    {
        var folder = typeof(UIConfig).GetStandardFolder(Environment.SpecialFolder.ApplicationData);
        if(!folder.Exists)
            Directory.CreateDirectory(folder.FullName);
        return Path.Combine(folder.FullName, LogFolder, LogFileName);
    }
    
    public static string GetConfigPath(bool testlive)
    {
        var folder = typeof(UIConfig).GetStandardFolder(Environment.SpecialFolder.ApplicationData);
        if(!folder.Exists)
            Directory.CreateDirectory(folder.FullName);
        return Path.Combine(folder.FullName, testlive ? Constants.FileTestLiveConfig : Constants.FileLiveConfig);
    }
        
    public static string GetUIConfigPath()
    {
        var folder = typeof(UIConfig).GetStandardFolder(Environment.SpecialFolder.ApplicationData);
        if(!folder.Exists)
            Directory.CreateDirectory(folder.FullName);
        return Path.Combine(folder.FullName, ConfigFileName);
    }
}