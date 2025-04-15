using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public static bool ValidateGameDirectory(string gameDirectory, out string errorMessage)
    {
        if (string.IsNullOrEmpty(gameDirectory))
        {
            errorMessage = Resources.InvalidDirectory;
            return false;
        }
        if (!Directory.Exists(gameDirectory))
        {
            errorMessage = Resources.DirectoryNotFound;
            return false;
        }
        if (!File.Exists(Path.Join(gameDirectory, Constants.FolderGameBinaries, Constants.FileClientBin)))
        {
            errorMessage = Resources.GameDirectoryInvalidError;
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }
        
    public static void RestartProcess(AppSetup setup, bool asAdmin = false)
    {
        var data = Tools.GetProcess(Environment.ProcessId);
        var version = setup.IsTestLive ? Constants.argTestLive : Constants.argLive;
        if (!data.args.Contains(version))
            data.args += version;
            
        Process process = new Process();
        process.StartInfo.FileName = data.filename;
        process.StartInfo.Arguments = data.args;
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

    [Obsolete]
    public static async Task<bool> SingleAppInstanceLock()
    {
        var process = Process.GetCurrentProcess();
        var module = process.MainModule;
        if (module is null) throw new Exception(@"MainModule is invalid");
        var filename = module.FileName;
        if (!File.Exists(filename)) throw new FileNotFoundException(@"App file not found");
        var processDetails = await Tools.GetProcessesWithName(Path.GetFileName(filename));
            
        foreach (var details in processDetails)
        {
            if(!string.Equals(Path.GetFullPath(details.filename), Path.GetFullPath(filename))) continue;
            if(details.pid == process.Id) continue;
            if (details.TryGetProcess(out var otherProcess))
            {
                Tools.FocusWindow(otherProcess.MainWindowHandle);
                ShutdownDesktopProcess();
                return false;
            }
        }

        return true;
    }    
        
    public static IEnumerable<(ulong, ulong)> GetManifestKeyValuePairs(this List<PublishedFile> list)
    {
        foreach (var file in list)
        {
            if (ulong.TryParse(file.HcontentFile, out var manifest))
                yield return (file.PublishedFileID, manifest);
        }
    }

    public static async Task<string> GetClipBoard()
    {
        if (Application.Current is null ||
            Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return string.Empty;
        if (desktop.MainWindow?.Clipboard is null) 
            return string.Empty;
        return await desktop.MainWindow.Clipboard.GetTextAsync()  ?? string.Empty;
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