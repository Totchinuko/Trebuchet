using GoogLib;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Goog
{
    public static class Setup
    {
        public const string SteamCMDURL = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        public const string steamCMDZipFile = "SteamCMD.zip";
        public static readonly Regex AppInfoBuildID = new Regex("\"branches\"{\"public\"{\"buildid\"\"([0-9]+)\"\"timeupdated\"\"([0-9]+)\"}");
        public static readonly Regex ManifestBuildID = new Regex("\"buildid\"\"([0-9]+)\"");

        public static void DeleteSteamCMD(Config config)
        {
            Tools.DeleteIfExists(Path.Combine(config.InstallPath, Config.FolderSteam));
        }

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

        public static string GetGoogCMD()
        {
            string? appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (appFolder == null)
                throw new Exception("App is installed in an invalid folder.");
            string GoogCMD = Path.Combine(appFolder, "GoogCMD", "Goog.exe");
            if (!File.Exists(GoogCMD))
                throw new FileNotFoundException($"{GoogCMD} was not found.");
            return GoogCMD;
        }

        public static int GetInstalledInstances(Config config)
        {
            int count = 0;

            string folder = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances);
            if (!Directory.Exists(folder))
                return 0;

            string[] instances = Directory.GetDirectories(folder);
            foreach (string instance in instances)
            {
                string bin = Path.Combine(instance, Config.FolderGameBinaries, Config.FileServerBin);
                if (File.Exists(bin))
                    count++;
            }

            return count;
        }

        public static ulong GetInstanceBuildID(Config config, int instance)
        {
            string manifest = Path.Combine(
                config.InstallPath,
                config.VersionFolder,
                Config.FolderServerInstances,
                string.Format(Config.FolderInstancePattern, instance),
                string.Format(Config.FileSteamInstanceManifeste, config.ServerAppID.ToString()));

            if (!File.Exists(manifest)) throw new Exception($"Server instance {instance} is not installed properly.");

            string content = File.ReadAllText(manifest);
            foreach (string search in new string[] { " ", "\n", "\r", "\n\r", "\t" })
                content = content.Replace(search, string.Empty);
            var match = ManifestBuildID.Match(content);

            if (!match.Success) throw new Exception($"Could not parse the manifest to find a build id");

            if (!ulong.TryParse(match.Groups[1].Value, out ulong buildID))
                return buildID;
            return 0;
        }

        public static async Task<UpdateCheckEventArgs> GetServerUptoDate(Config config, CancellationToken ct)
        {
            if (config.ServerInstanceCount <= 0) return new UpdateCheckEventArgs();

            ulong instance0 = GetInstanceBuildID(config, 0);
            ulong steam = await GetSteamBuildID(config, ct);

            return new UpdateCheckEventArgs
            {
                IsUpToDate = instance0 == steam,
                steamBuildID = steam,
                currentBuildID = instance0
            };
        }

        public static async Task<ulong> GetSteamBuildID(Config config, CancellationToken ct)
        {
            string appinfo = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamAppInfo);

            if (File.Exists(appinfo))
                File.Move(appinfo, appinfo + ".changed");

            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin);
            process.StartInfo.Arguments = string.Join(" ",
                    Config.CmdArgLoginAnonymous,
                    "+app_info_update 1",
                    "+app_info_print " + config.ServerAppID,
                    Config.CmdArgQuit
                );
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            string content = await WaitForProcessAnswer(process, ct);

            foreach (string search in new string[] { " ", "\n", "\r", "\n\r", "\t" })
                content = content.Replace(search, string.Empty);
            var match = AppInfoBuildID.Match(content);

            if (!match.Success) throw new Exception($"Could not parse the manifest to find a build id");
            if (!ulong.TryParse(match.Groups[1].Value, out ulong buildID))
                return buildID;
            return 0;
        }

        public static void RemoveAllSymbolicLinks(Config config)
        {
            string folder = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances);
            if (!Directory.Exists(folder))
                return;
            string[] instances = Directory.GetDirectories(folder);
            foreach (string instance in instances)
                Tools.RemoveSymboliclink(Path.Combine(instance, Config.FolderGameSave));
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
            Setup.RemoveAllSymbolicLinks(config);
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

        public static async Task<int> SetupAppAndSteam(Config config, CancellationToken token, bool reinstall = false)
        {
            await SetupApp(config, token, reinstall);
            return await UpdateSteamCMD(config, token);
        }

        public static async Task<int> UpdateMods(Config config, string modlist, CancellationToken token)
        {
            string modlistFile = ModListProfile.GetPath(config, modlist);
            if (!File.Exists(modlistFile))
                throw new FileNotFoundException($"modlist {modlist} was not found.");

            ModListProfile modlistProfile = ModListProfile.LoadProfile(config, modlistFile);

            var list = modlistProfile.GetModIDList().Select(mod => string.Format(Config.CmdArgWorkshopUpdate, config.ClientAppID, mod));
            var update = string.Join(" ", list);

            if (update.Length == 0) return 0;

            string steamArgs = string.Join(" ",
                    Config.CmdArgLoginAnonymous,
                    update,
                    Config.CmdArgQuit
                );

            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin);
            process.StartInfo.Arguments = steamArgs;
            process.StartInfo.CreateNoWindow = !config.DisplayCMD;
            process.StartInfo.UseShellExecute = false;
            return await WaitForProcess(process, token);
        }

        public static async Task<int> UpdateServer(Config config, int instanceNumber, CancellationToken token, bool reinstall = false)
        {
            if (config.ServerInstanceCount <= 0) return 0;

            string instance = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, instanceNumber));

            if (reinstall)
            {
                Tools.RemoveSymboliclink(Path.Combine(instance, Config.FolderGameSave));
                Tools.DeleteIfExists(instance);
            }

            Tools.CreateDir(instance);

            string steamArgs = string.Join(" ",
                    string.Format(Config.CmdArgForceInstallDir, instance),
                    Config.CmdArgLoginAnonymous,
                    string.Format(Config.CmdArgAppUpdate, config.ServerAppID),
                    Config.CmdArgQuit
                );

            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin);
            process.StartInfo.Arguments = steamArgs;
            process.StartInfo.CreateNoWindow = !config.DisplayCMD;
            process.StartInfo.UseShellExecute = false;
            return await WaitForProcess(process, token);
        }

        public static async Task UpdateServerFromInstance0(Config config, int instanceNumber, CancellationToken token)
        {
            if (config.ServerInstanceCount <= 0) return;
            if (instanceNumber == 0)
                throw new Exception("Can't update instance 0 with itself.");

            string instance = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, instanceNumber));
            string instance0 = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, 0));

            if (!Directory.Exists(instance0))
                throw new DirectoryNotFoundException($"{instance0} was not found.");

            Tools.RemoveSymboliclink(Path.Combine(instance, Config.FolderGameSave));
            Tools.DeleteIfExists(instance);
            Tools.CreateDir(instance);

            await Tools.DeepCopyAsync(instance0, instance, token);
        }

        public static async Task<int> UpdateSteamCMD(Config config, CancellationToken token)
        {
            string steamArgs = string.Join(" ",
                    Config.CmdArgLoginAnonymous,
                    Config.CmdArgQuit
                );

            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin);
            process.StartInfo.Arguments = steamArgs;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = !config.DisplayCMD;
            return await WaitForProcess(process, token);
        }

        public static async Task<int> WaitForProcess(Process process, CancellationToken token)
        {
            process.Start();
            while (!process.HasExited && !token.IsCancellationRequested)
                await Task.Delay(200);
            if (process.HasExited)
            {
                int error = process.ExitCode;
                process.Dispose();
                return error != 0 && error != 7 ? 1 : 0;
            }

            process.Kill(true);
            process.WaitForExit();
            process.Dispose();
            return 0;
        }

        public static async Task<string> WaitForProcessAnswer(Process process, CancellationToken ct)
        {
            process.Start();
            string content = string.Empty;
            while (!process.HasExited && !ct.IsCancellationRequested)
            {
                content += process.StandardOutput.ReadToEnd();
                await Task.Delay(200);
            }
            if (process.HasExited)
            {
                process.Dispose();
                return content;
            }

            process.Kill(true);
            process.WaitForExit();
            process.Dispose();
            return content;
        }
    }
}