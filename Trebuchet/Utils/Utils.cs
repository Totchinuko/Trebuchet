using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Styling;
using SteamWorksWebAPI;
using Trebuchet.Assets;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.Utils;

internal static class Utils
{
    public static void RestartProcess(this AppSetup setup, bool asAdmin = false)
    {
        var data = Tools.GetProcess(Environment.ProcessId);
        var version = setup.IsTestLive ? Constants.argTestLive : Constants.argLive;
        List<string> arguments = data.args.Split(' ').ToList();
        if (!arguments.Contains(version))
            arguments.Add(version);
        if(!arguments.Contains(AppConstants.RestartArg))
            arguments.Add(AppConstants.RestartArg);
            
        Process process = new Process();
        process.StartInfo.FileName = data.filename;
        process.StartInfo.Arguments = string.Join(' ', arguments);
        process.StartInfo.UseShellExecute = true;
        if (asAdmin)
            process.StartInfo.Verb = "runas";
        process.Start();
        ShutdownDesktopProcess();
    }

    public static void ShutdownDesktopProcess()
    {
        if(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
    
    public static void ApplyPlateformTheme(PlateformTheme theme)
    {
        if (Application.Current is null) return;
        
        switch (theme)
        {
            case PlateformTheme.Default:
                Application.Current.RequestedThemeVariant = ThemeVariant.Default;
                return;
            case PlateformTheme.Dark:
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
                return;
            case PlateformTheme.Light:
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                return;
        }
    }
}