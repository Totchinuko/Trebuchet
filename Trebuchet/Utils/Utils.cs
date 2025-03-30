using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Trebuchet.Assets;
using TrebuchetLib;

namespace Trebuchet.Utils
{
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
                errorMessage = Resources.InvalidDirectory;
                return false;
            }
            if (!Directory.Exists(installPath))
            {
                errorMessage = Resources.DirectoryNotFound;
                return false;
            }
            if (Regex.IsMatch(installPath, Constants.RegexSavedFolder))
            {
                errorMessage = Resources.InstallPathInGameError;
                return false;
            }
            return true;
        }
    }
}