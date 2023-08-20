using GoogLib;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Goog
{
    public static class Setup
    {
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
                Config.FileBuildID);

            if (!File.Exists(manifest))
                return 0;

            string content = File.ReadAllText(manifest);
            if (!ulong.TryParse(content, out ulong buildID))
                return buildID;
            return 0;
        }


        /// <summary>
        /// Force refresh of the steam app info cache and get the current build id of the server app.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="steam"></param>
        /// <returns></returns>
        public static ulong GetSteamBuildID(Config config, SteamSession steam)
        {
            steam.RequestAppInfo(config.ServerAppID, true);
            return steam.ContentDownloader.GetSteam3AppBuildNumber(config.ServerAppID, ContentDownloader.DEFAULT_BRANCH);
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
        /// Update a list of mods from the steam workshop.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="steam"></param>
        /// <param name="enumerable"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public static async Task UpdateMods(Config config, SteamSession steam, IEnumerable<ulong> enumerable, CancellationTokenSource cts)
        {
            steam.ContentDownloader.SetInstallDirectory(Path.Combine(config.InstallPath, Config.FolderWorkshop));
            await steam.ContentDownloader.DownloadUGCAsync(config.ClientAppID, enumerable, ContentDownloader.DEFAULT_BRANCH, cts);
        }


        /// <summary>
        /// Performs a server update on a targeted instance.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instanceNumber"></param>
        /// <param name="token"></param>
        /// <param name="reinstall"></param>
        /// <returns></returns>
        public static async Task UpdateServer(Config config, SteamSession steam, int instanceNumber, CancellationTokenSource cts, bool reinstall = false)
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

            steam.ContentDownloader.SetInstallDirectory(instance);
            await steam.ContentDownloader.DownloadAppAsync(config.ServerAppID, new List<(uint depotId, ulong manifestId)>(), ContentDownloader.DEFAULT_BRANCH, null, null, null, false, cts);
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
        public static async Task UpdateServerInstances(Config config, SteamSession steam, CancellationTokenSource cts)
        {
            Log.Write("Updating all server instances...", LogSeverity.Info);
            int count = config.ServerInstanceCount;
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    await UpdateServer(config, steam, i, cts, false);
                }
                else
                {
                    await UpdateServerFromInstance0(config, i, cts.Token);
                }
            }
        }
    }
}