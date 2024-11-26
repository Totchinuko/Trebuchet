using DepotDownloader;
using SteamKit2;
using TrebuchetUtils;

namespace Trebuchet
{
    public class Steam : IDebugListener
    {
        private Config _config;
        private Steam3Session _session;

        public Steam(Config config)
        {
            _config = config;

            SteamKit2.DebugLog.AddListener(this);
            Util.ConsoleWriteRedirect += OnConsoleWriteRedirect;
            AccountSettingsStore.LoadFromFile(Path.Combine(_config.ResolvedInstallPath, _config.VersionFolder, "account.config"));
            ContentDownloader.Config.RememberPassword = false;
            ContentDownloader.Config.DownloadManifestOnly = false;
            ContentDownloader.Config.CellID = 0; //TODO: Offer regional download selection
            ContentDownloader.Config.MaxDownloads = _config.MaxDownloads;
            ContentDownloader.Config.MaxServers = Math.Max(_config.MaxServers, ContentDownloader.Config.MaxDownloads);
            ContentDownloader.Config.LoginID = null;
            ContentDownloader.Config.DepotConfigDirectory = Path.Combine(_config.ResolvedInstallPath, Config.FolderWorkshop, ContentDownloader.CONFIG_DIR);
            _session = ContentDownloader.InitializeSteam3(null, null);
            _session.Connected += (sender, args) => Connected?.Invoke(this, EventArgs.Empty);
            _session.Disconnected += (sender, args) => Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? Connected;
        public event EventHandler? Disconnected;
        public bool IsConnected => _session?.steamClient?.IsConnected ?? false;

        public void ClearCache()
        {
            Tools.DeleteIfExists(ContentDownloader.Config.DepotConfigDirectory);
        }

        public async void Connect()
        {
            await _session.Connect();
        }

        public void Disconnect(bool sendLogOff = true)
        {
            _session.Disconnect(sendLogOff);
        }

        /// <summary>
        /// Count the amount of instances currently installed. This does not verify if the files are valid, just the main binary.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public int GetInstalledInstances()
        {
            int count = 0;

            string folder = Path.Combine(_config.ResolvedInstallPath, _config.VersionFolder, Config.FolderServerInstances);
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
        public ulong GetInstanceBuildID(int instance)
        {
            string manifest = Path.Combine(
                _config.ResolvedInstallPath,
                _config.VersionFolder,
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

        public string GetServerInstancePath(int instanceNumber)
        {
            return Path.Combine(_config.ResolvedInstallPath, _config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, instanceNumber));
        }

        public uint GetSteam3AppBuildNumber(uint appId, string branch)
        {
            if (appId == ContentDownloader.INVALID_APP_ID)
                return 0;

            var depots = ContentDownloader.GetSteam3AppSection(appId, EAppInfoSection.Depots);
            var branches = depots["branches"];
            var node = branches[branch];

            if (node == KeyValue.Invalid)
                return 0;

            var buildid = node["buildid"];

            if (buildid == KeyValue.Invalid || buildid.Value == null)
                return 0;

            return uint.Parse(buildid.Value);
        }

        /// <summary>
        /// Force refresh of the steam app info cache and get the current build id of the server app.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="steam"></param>
        /// <returns></returns>
        public async Task<ulong> GetSteamBuildID()
        {
            if (_session == null)
                throw new InvalidOperationException("Steam session is not functioning");
            return await Task.Run(() =>
            {
                _session.RequestAppInfo(_config.ServerAppID, true);
                return ContentDownloader.GetSteam3AppBuildNumber(_config.ServerAppID, ContentDownloader.DEFAULT_BRANCH);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Compare a list of manifest ID from Published Files with the local steam cache
        /// </summary>
        /// <param name="keyValuePairs"></param>
        /// <returns></returns>
        public IEnumerable<ulong> GetUpdatedUGCFileIDs(IEnumerable<(ulong pubID, ulong manisfestID)> keyValuePairs)
        {
            var depotConfigStore = DepotConfigStore.LoadInstanceFromFile(Path.Combine(_config.ResolvedInstallPath, Config.FolderWorkshop, ContentDownloader.CONFIG_DIR, ContentDownloader.DEPOT_CONFIG));
            foreach (var (pubID, manisfestID) in keyValuePairs)
            {
                if (!depotConfigStore.InstalledUGCManifestIDs.TryGetValue(pubID, out ulong manisfest))
                    yield return pubID;
                if (manisfest != manisfestID)
                    yield return pubID;
            }
        }

        /// <summary>
        /// Remove all junctions present in any server instance folder. Used before update to avoid crash du to copy error on junction folders.
        /// </summary>
        /// <param name="config"></param>
        public void RemoveAllSymbolicLinks()
        {
            string folder = Path.Combine(_config.ResolvedInstallPath, _config.VersionFolder, Config.FolderServerInstances);
            if (!Directory.Exists(folder))
                return;
            string[] instances = Directory.GetDirectories(folder);
            foreach (string instance in instances)
                Tools.RemoveSymboliclink(Path.Combine(instance, Config.FolderGameSave));
        }

        public void SetProgress(IProgress<double> progress)
        {
            ContentDownloader.Config.Progress = progress;
        }

        public bool SetupFolders()
        {
            if (!Tools.ValidateInstallDirectory(_config.ResolvedInstallPath)) return false;

            try
            {
                Tools.CreateDir(Path.Combine(_config.ResolvedInstallPath, _config.VersionFolder, Config.FolderServerInstances));
                Tools.CreateDir(Path.Combine(_config.ResolvedInstallPath, _config.VersionFolder, Config.FolderClientProfiles));
                Tools.CreateDir(Path.Combine(_config.ResolvedInstallPath, _config.VersionFolder, Config.FolderServerProfiles));
                Tools.CreateDir(Path.Combine(_config.ResolvedInstallPath, _config.VersionFolder, Config.FolderModlistProfiles));

                Tools.CreateDir(Path.Combine(_config.ResolvedInstallPath, Config.FolderWorkshop));
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                throw new Exception("Failed to create app folders.", ex);
            }

            return true;
        }

        /// <summary>
        /// Update a list of mods from the steam workshop.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="steam"></param>
        /// <param name="enumerable"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task UpdateMods(IEnumerable<ulong> enumerable, CancellationTokenSource cts)
        {
            if (!SetupFolders()) return;
            if (!await WaitSteamConnectionAsync()) return;

            ContentDownloader.Config.InstallDirectory = Path.Combine(_config.ResolvedInstallPath, Config.FolderWorkshop);

            await ContentDownloader.DownloadUGCAsync(new uint[] { Config.AppIDLiveClient, Config.AppIDTestLiveClient }, enumerable, ContentDownloader.DEFAULT_BRANCH, cts);
        }

        /// <summary>
        /// Performs a server update on a targeted instance.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instanceNumber"></param>
        /// <param name="token"></param>
        /// <param name="reinstall"></param>
        /// <returns></returns>
        public async Task UpdateServer(int instanceNumber, CancellationTokenSource cts, bool reinstall = false)
        {
            if (_config.ServerInstanceCount <= 0) return;

            if (!SetupFolders()) return;

            if (!await WaitSteamConnectionAsync().ConfigureAwait(false)) return;

            await Log.Write($"Updating server instance {instanceNumber}.", LogSeverity.Info).ConfigureAwait(false);

            string instance = GetServerInstancePath(instanceNumber);
            if (reinstall)
            {
                Tools.RemoveSymboliclink(Path.Combine(instance, Config.FolderGameSave));
                Tools.DeleteIfExists(instance);
            }

            Tools.CreateDir(instance);
            ContentDownloader.Config.InstallDirectory = instance;
            await Task.Run(() => ContentDownloader.DownloadAppAsync(_config.ServerAppID, new List<(uint depotId, ulong manifestId)>(), ContentDownloader.DEFAULT_BRANCH, null, null, null, false, false, cts), cts.Token).ConfigureAwait(false);
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
        public async Task UpdateServerFromInstance0(int instanceNumber, CancellationToken token)
        {
            if (_config.ServerInstanceCount <= 0) return;
            if (instanceNumber == 0)
                throw new Exception("Can't update instance 0 with itself.");

            await Log.Write("Updating server instance {0} from instance 0.", LogSeverity.Info);
            if (!SetupFolders()) return;

            string instance = Path.Combine(_config.ResolvedInstallPath, _config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, instanceNumber));
            string instance0 = Path.Combine(_config.ResolvedInstallPath, _config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, 0));

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
        public async Task UpdateServerInstances(CancellationTokenSource cts)
        {
            if (_config.ServerInstanceCount <= 0) return;

            if (!await WaitSteamConnectionAsync()) return;

            await Log.Write("Updating all server instances...", LogSeverity.Info);
            int count = _config.ServerInstanceCount;
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    await UpdateServer(i, cts, false);
                }
                else
                {
                    await UpdateServerFromInstance0(i, cts.Token);
                }
            }
        }

        public bool WaitSteamConnection()
        {
            var task = Task.Run(WaitSteamConnectionAsync);
            task.Wait();
            return task.Result;
        }

        public async Task<bool> WaitSteamConnectionAsync()
        {
            if (IsConnected) return true;
            if (_session == null) return false;
            await _session.Connect();
            return IsConnected;
        }

        public async void WriteLine(string category, string msg)
        {
            await Log.Write($"[{category}] {msg}", LogSeverity.Info);
        }

        private void OnConsoleWriteRedirect(string obj)
        {
            obj = obj.Replace(Environment.NewLine, string.Empty);
            Log.Write($"[ContentDownloader] {obj}", LogSeverity.Info);
        }
    }
}