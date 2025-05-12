using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using tot_lib;
using tot_gui_lib;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.Utils;

public static class AppConstants
{
    public const string ConfigFileName = "settings.ui.json";
    public const string GithubOwnerUpdate = "Totchinuko";
    public const string GithubRepoUpdate = "Trebuchet";
    public const string AutoStartLive = "TotTrebuchetLive";
    public const string AutoStartTestLive = "TotTrebuchetTestLive";

    public const string RestartArg = "--restart";

    [Localizable(false)]
    public static readonly string[] UICultureList = ["en", "fr", "de"];

    public static string GetUIConfigPath()
    {
        var folder = AppSetup.GetAppConfigDirectory();
        if(!folder.Exists)
            Directory.CreateDirectory(folder.FullName);
        return Path.Combine(folder.FullName, ConfigFileName);
    }

    public static string GetUpdateContentType()
    {
        if (OperatingSystem.IsWindows()) return GithubUpdater.WindowsMimeType;
        return string.Empty;
    }
}