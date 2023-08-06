using Goog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Goog
{
    public static class Setup
    {
        public const string SteamCMDURL = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        public const string steamCMDZipFile = "SteamCMD.zip";

        public static async Task DownloadFile(string url, string file, CancellationToken token, IProgress<float>? progress = null)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Download URL is empty.");

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);

                using (FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write))
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

            string steamFolder = Path.Join(config.InstallPath, Config.FolderSteam);
            string steamCMD = Path.Join(steamFolder, Config.FileSteamCMDBin);
            if (File.Exists(steamCMD) && !reinstall)
                throw new Exception("Install already exists.");

            var installPath = config.InstallPath;
            Tools.CreateDir(installPath);
            Tools.DeleteIfExists(steamFolder);
            Tools.CreateDir(steamFolder);
            config.RemoveAllSymbolicLinks();
            Tools.DeleteIfExists(Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances));

            string steamCmdZip = Path.Combine(installPath, steamCMDZipFile);
            Tools.DeleteIfExists(steamCmdZip);

            await DownloadFile(SteamCMDURL, steamCmdZip, token, null);

            await Task.Run(() => Tools.UnzipFile(steamCmdZip, steamFolder), token);

            FileInfo steamExe = new FileInfo(Path.Combine(steamFolder, Config.FileSteamCMDBin));
            if (!steamExe.Exists)
                throw new FileNotFoundException($"{steamExe.FullName} was not found after extraction.");

            Tools.DeleteIfExists(steamCmdZip);
        }

        public static async Task<int> UpdateServer(Config config, CancellationToken token, bool reinstall = false)
        {
            if (config.ServerInstanceCount <= 0) return 1;

            string instancesFolder = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances);
            string steamCMD = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin);

            if (reinstall)
            {
                config.RemoveAllSymbolicLinks();
                Tools.DeleteIfExists(instancesFolder);
            }

            config.CreateInstanceDirectories();

            string[] folders = Directory.GetDirectories(instancesFolder);
            foreach(string instance in folders)
            {
                Process process = new Process();
                process.StartInfo.FileName = steamCMD;
                process.StartInfo.Arguments = string.Join(" ",
                        string.Format(Config.CmdArgForceInstallDir, instance),
                        Config.CmdArgLoginAnonymous,
                        string.Format(Config.CmdArgAppUpdate, config.ServerAppID),
                        Config.CmdArgQuit
                    );
                int code = await WaitForSteamCMD(process, token);
                if (code != 7)
                    return code;
            }
            return 0;
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