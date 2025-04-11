using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using tot_lib;
using TrebuchetUtils;
using TrebuchetLib;

namespace Trebuchet.Utils;

public static class AppConstants
{
    public const string ConfigFileName = "settings.ui.json";

    [Localizable(false)]
    public static readonly string[] UICultureList = ["en", "fr"];

    public static string GetUIConfigPath()
    {
        var folder = typeof(UIConfig).GetStandardFolder(Environment.SpecialFolder.ApplicationData);
        if(!folder.Exists)
            Directory.CreateDirectory(folder.FullName);
        return Path.Combine(folder.FullName, ConfigFileName);
    }
}