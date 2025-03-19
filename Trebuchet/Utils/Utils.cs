using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using SteamWorksWebAPI;
using TrebuchetLib;
using TrebuchetUtils.Modals;

namespace Trebuchet.Utils
{
    internal static class Utils
    {
        public static bool ValidateGameDirectory(string gameDirectory, out string errorMessage)
        {
            if (string.IsNullOrEmpty(gameDirectory))
            {
                errorMessage = App.GetAppText("InvalidDirectory");
                return false;
            }
            if (!Directory.Exists(gameDirectory))
            {
                errorMessage = App.GetAppText("DirectoryNotFound");
                return false;
            }
            if (!File.Exists(Path.Join(gameDirectory, Config.FolderGameBinaries, Config.FileClientBin)))
            {
                errorMessage = App.GetAppText("GameDirectoryInvalidError");
                return false;
            }
            errorMessage = string.Empty;
            return true;
        }
        
        public static void RestartProcess(bool testlive, bool asAdmin = false)
        {
            var data = Tools.GetProcess(Environment.ProcessId);
            Process process = new Process();
            process.StartInfo.FileName = data.filename;
            process.StartInfo.Arguments = data.args + (testlive ? " -testlive" : " -live");
            process.StartInfo.UseShellExecute = true;
            if (asAdmin)
                process.StartInfo.Verb = "runas";
            process.Start();
            if(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        }

        public static bool ValidateInstallDirectory(string installPath, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(installPath))
            {
                errorMessage = App.GetAppText("InvalidDirectory");
                return false;
            }
            if (!Directory.Exists(installPath))
            {
                errorMessage = App.GetAppText("DirectoryNotFound");
                return false;
            }
            if (Regex.IsMatch(installPath, Config.RegexSavedFolder))
            {
                errorMessage = App.GetAppText("InstallPathInGameError");
                return false;
            }
            return true;
        }
    }
}