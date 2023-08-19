using GoogLib;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Goog
{
    public static class Setup
    {
        public const string SteamCMDURL = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        public const string steamCMDZipFile = "SteamCMD.zip";
        public static readonly Regex AppInfoBuildID = new Regex("\"branches\"{\"public\"{\"buildid\"\"([0-9]+)\"\"timeupdated\"\"([0-9]+)\"}");
        public static readonly Regex ManifestBuildID = new Regex("\"buildid\"\"([0-9]+)\"");

        /// <summary>
        /// Download a file from a url. This is mostly used once to download steam cmd during setup.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="file"></param>
        /// <param name="token"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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

        /// <summary>
        /// Count the amount of instances currently installed. This does not verify if the files are valid, just the main binary.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get the installed local build id of a specified instance
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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

        /// <summary>
        /// Check if the server is up to date
        /// </summary>
        /// <param name="config"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<UpdateCheckEventArgs> GetServerUptoDate(Config config, CancellationToken ct)
        {
            if (config.ServerInstanceCount <= 0)
                throw new Exception("No server instance is configured.");

            Log.Write("Checking for server updates...", LogSeverity.Info);

            ulong instance0;
            ulong steam;
            try
            {
                instance0 = GetInstanceBuildID(config, 0);
                steam = await GetSteamBuildID(config, ct);
            }
            catch (Exception ex)
            {
                return new UpdateCheckEventArgs(0, 0, ex);
            }

            return new UpdateCheckEventArgs(instance0, steam);
        }

        /// <summary>
        /// Request the current build id of the game on steam
        /// </summary>
        /// <param name="config"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<ulong> GetSteamBuildID(Config config, CancellationToken ct)
        {
            string appinfo = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamAppInfo);

            Tools.DeleteIfExists(appinfo);

            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin);
            process.StartInfo.Arguments = string.Join(" ",
                    string.Format(Config.CmdArgForceInstallDir, Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, 0))),
                    Config.CmdArgLoginAnonymous,
                    "+app_info_update 1",
                    "+app_info_print " + config.ServerAppID,
                    Config.CmdArgQuit
                );
            process.StartInfo.CreateNoWindow = true;
            string content = await WaitForProcessAnswer(process, ct);

            foreach (string search in new string[] { " ", "\n", "\r", "\n\r", "\t" })
                content = content.Replace(search, string.Empty);
            var match = AppInfoBuildID.Match(content);

            if (!match.Success) throw new Exception($"Could not parse the manifest to find a build id");
            if (!ulong.TryParse(match.Groups[1].Value, out ulong buildID))
                return buildID;
            return 0;
        }

        /// <summary>
        /// Remove all junctions present in any server instance folder. Used before update to avoid crash du to copy error on junction folders.
        /// </summary>
        /// <param name="config"></param>
        public static void RemoveAllSymbolicLinks(Config config)
        {
            string folder = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances);
            if (!Directory.Exists(folder))
                return;
            string[] instances = Directory.GetDirectories(folder);
            foreach (string instance in instances)
                Tools.RemoveSymboliclink(Path.Combine(instance, Config.FolderGameSave));
        }

        /// <summary>
        /// Setup the server. This will download steamcmd and unzip it.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public static async Task SetupApp(Config config, CancellationToken token)
        {
            Log.Write($"Download SteamCMD...", LogSeverity.Info);
            if (string.IsNullOrEmpty(config.InstallPath))
                throw new Exception("install-path is not configured properly.");

            if (string.IsNullOrEmpty(SteamCMDURL))
                throw new Exception("URL is invalid");

            string steamFolder = Path.Join(config.InstallPath, Config.FolderSteam);
            string steamCMD = Path.Join(steamFolder, Config.FileSteamCMDBin);

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

        /// <summary>
        /// Download steamcmd main binary and execute it for installation.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<int> SetupAppAndSteam(Config config, CancellationToken token)
        {
            await SetupApp(config, token);
            return await UpdateSteamCMD(config, token);
        }

        /// <summary>
        /// Performs a steamcmd update on the provided modlist and wait for the process to finish. Returns 1 if the opperation failed.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="modlist">Existing modlist name</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<int> UpdateMods(Config config, string modlist, CancellationToken token)
        {
            string modlistFile = ModListProfile.GetPath(config, modlist);
            if (!File.Exists(modlistFile))
                throw new FileNotFoundException($"modlist {modlist} was not found.");

            ModListProfile modlistProfile = ModListProfile.LoadProfile(config, modlistFile);

            var list = modlistProfile.GetModIDList().Select(mod => string.Format(Config.CmdArgWorkshopUpdate, config.ClientAppID, mod));
            var update = string.Join(" ", list);

            if (update.Length == 0) return 0;
            Log.Write($"Updating mods in list {modlist}", LogSeverity.Info);

            string steamArgs = string.Join(" ",
                    Config.CmdArgLoginAnonymous,
                    update,
                    Config.CmdArgQuit
                );

            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin);
            process.StartInfo.Arguments = steamArgs;
            process.StartInfo.CreateNoWindow = !config.DisplayCMD;
            return await WaitForProcess(process, token);
        }

        /// <summary>
        /// Performs a steamcmd update on the provided modlist and wait for the process to finish. Returns 1 if the opperation failed.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="ids"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<int> UpdateMods(Config config, IEnumerable<ulong> ids, CancellationToken ct)
        {
            var update = string.Join(" ", ids.Select(i => string.Format(Config.CmdArgWorkshopUpdate, config.ClientAppID, i.ToString())));

            if (update.Length == 0) return 0;
            Log.Write($"Updating {ids.Count()} mods...", LogSeverity.Info);

            string steamArgs = string.Join(" ",
                    Config.CmdArgLoginAnonymous,
                    update,
                    Config.CmdArgQuit
                );

            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin);
            process.StartInfo.Arguments = steamArgs;
            process.StartInfo.CreateNoWindow = !config.DisplayCMD;
            return await WaitForProcess(process, ct);
        }

        /// <summary>
        /// Performs a server update on a targeted instance.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instanceNumber"></param>
        /// <param name="token"></param>
        /// <param name="reinstall"></param>
        /// <returns></returns>
        public static async Task<int> UpdateServer(Config config, int instanceNumber, CancellationToken token, bool reinstall = false)
        {
            if (config.ServerInstanceCount <= 0)
                throw new Exception("No server instance is configured.");

            Log.Write($"Updating server instance {instanceNumber}.", LogSeverity.Info);

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
            return await WaitForProcess(process, token);
        }

        /// <summary>
        /// Update a server instance other than 0, from the content of instance 0.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instanceNumber"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public static async Task UpdateServerFromInstance0(Config config, int instanceNumber, CancellationToken token)
        {
            Log.Write("Updating server instance {0} from instance 0.", LogSeverity.Info);
            if (config.ServerInstanceCount <= 0) return;
            if (instanceNumber == 0)
                throw new Exception("Can't update instance 0 with itself.");

            string instance = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, instanceNumber));
            string instance0 = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, 0));

            if (!Directory.Exists(instance0))
                throw new DirectoryNotFoundException($"{instance0} was not found.");

            Tools.RemoveSymboliclink(Path.Combine(instance0, Config.FolderGameSave));
            Tools.RemoveSymboliclink(Path.Combine(instance, Config.FolderGameSave));
            Tools.DeleteIfExists(instance);
            Tools.CreateDir(instance);

            await Tools.DeepCopyAsync(instance0, instance, token);
        }

        /// <summary>
        /// Update instance 0 using steamcmd, then copy the files of instance0 to update other instances.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<int> UpdateServerInstances(Config config, CancellationToken token)
        {
            Log.Write("Updating all server instances...", LogSeverity.Info);
            int count = config.ServerInstanceCount;
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    int code = await UpdateServer(config, i, token, false);
                    if (code != 0)
                        return code;
                }
                else
                {
                    await UpdateServerFromInstance0(config, i, token);
                }
            }
            return 0;
        }

        /// <summary>
        /// Update steamcmd to the latest version.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<int> UpdateSteamCMD(Config config, CancellationToken token)
        {
            Log.Write("Updating SteamCMD...", LogSeverity.Info);
            string steamArgs = string.Join(" ",
                    Config.CmdArgLoginAnonymous,
                    Config.CmdArgQuit
                );

            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin);
            process.StartInfo.Arguments = steamArgs;
            process.StartInfo.CreateNoWindow = !config.DisplayCMD;
            return await WaitForProcess(process, token);
        }

        /// <summary>
        /// Wait for a steamCMD process to exit, or kill it if the token is cancelled.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="token"></param>
        /// <returns>1 if the task failled, 0 otherwize</returns>
        public static async Task<int> WaitForProcess(Process process, CancellationToken token)
        {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            while (!process.HasExited && !token.IsCancellationRequested)
            {
                string? data = await process.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrEmpty(data))
                    Log.Write("Steam output" + Environment.NewLine + data, LogSeverity.Info);
                await Task.Delay(200);
            }
            if (process.HasExited)
            {
                int error = process.ExitCode;
                process.Dispose();
                Log.Write($"SteamCMD exited with code {error}", (error != 7 && error != 0 ? LogSeverity.Warning : LogSeverity.Info));
                return error != 0 && error != 7 ? 1 : 0;
            }

            Log.Write("SteamCMD process was killed by cancel.", LogSeverity.Warning);
            process.Kill(true);
            process.WaitForExit();
            process.Dispose();
            return 0;
        }

        /// <summary>
        /// Wait for a steam CMD process to exit while capturing its output, or kill it if the token is cancelled.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<string> WaitForProcessAnswer(Process process, CancellationToken ct)
        {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            StringBuilder message = new StringBuilder();
            while (true && !ct.IsCancellationRequested)
            {
                char? data = (char)process.StandardOutput.Read();
                message.Append(data);
                if (data == null)
                    break;
            }
            if (process.HasExited)
            {
                Log.Write($"SteamCMD exited with code {process.ExitCode}", (process.ExitCode != 7 && process.ExitCode != 0 ? LogSeverity.Warning : LogSeverity.Info));
                process.Dispose();
                return message.ToString();
            }

            Log.Write("SteamCMD process was killed by cancel.", LogSeverity.Warning);
            process.Kill(true);
            process.WaitForExit();
            process.Dispose();
            return message.ToString();
        }
    }
}