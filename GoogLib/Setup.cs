using Goog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog
{
    public static class Setup
    {
        public const string SteamCMDURL = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        public const string steamCMDZipFile = "SteamCMD.zip";

        public static async Task DownloadFile(string url, FileInfo file, CancellationToken token, IProgress<float>? progress = null)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Download URL is empty.");

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);

                using (FileStream fs = new FileStream(file.FullName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    await client.DownloadAsync(url, fs, progress, token);
                }
            }
        }

        public static async Task SetupApp(Config config, CancellationToken token, bool reinstall = false)
        {
            if (string.IsNullOrEmpty(config.InstallPath))
                throw new Exception("install-path is not configured properly.");

            if (string.IsNullOrEmpty(SteamCMDURL))
                throw new Exception("URL is invalid");

            if (config.SteamCMD.Exists && !reinstall)
                throw new Exception("Install already exists.");

            var installPath = new DirectoryInfo(config.InstallPath);
            Tools.CreateDir(installPath);
            Tools.DeleteIfExists(config.SteamFolder, true);
            Tools.CreateDir(config.SteamFolder);
            Tools.RemoveSymboliclink(config.ServerSaveFolder.FullName);
            Tools.DeleteIfExists(config.ServerFolder, true);

            FileInfo steamCmdZip = new FileInfo(Path.Join(installPath.FullName, steamCMDZipFile));
            Tools.DeleteIfExists(steamCmdZip);

            await DownloadFile(SteamCMDURL, steamCmdZip, token, null);

            steamCmdZip.Refresh();
            await Task.Run(() => Tools.UnzipFile(steamCmdZip, config.SteamFolder), token);

            FileInfo steamExe = new FileInfo(Path.Join(config.SteamFolder.FullName, Config.steamCMDBin));
            if (!steamExe.Exists)
                throw new FileNotFoundException($"{steamExe.FullName} was not found after extraction.");

            Tools.DeleteIfExists(steamCmdZip);
        }

        public static async Task<int> UpdateServer(Config config, CancellationToken token, bool reinstall = false)
        {
            if (!config.ManageServers) return 1;

            if (reinstall)
                Tools.DeleteIfExists(config.ServerFolder, true);

            Tools.CreateDir(config.ServerFolder);

            Process process = new Process();
            process.StartInfo.FileName = config.SteamCMD.FullName;
            process.StartInfo.Arguments = string.Join(" ",
                    string.Format(Config.CmdArgForceInstallDir, config.ServerFolder.FullName),
                    Config.CmdArgLoginAnonymous,
                    string.Format(Config.CmdArgAppUpdate, config.ServerAppID),
                    Config.CmdArgQuit
                );
            return await WaitForSteamCMD(process, token);
        }

        public static async Task<int> WaitForSteamCMD(Process process, CancellationToken token)
        {
            process.Start();
            while (!process.HasExited && !token.IsCancellationRequested)
                await Task.Delay(200);
            if (!process.HasExited)
            {
                process.Kill();
                return 1;
            }
            return process.ExitCode;
        }
    }
}