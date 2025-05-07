using DepotDownloader;
using Microsoft.Extensions.Logging;
using SteamKit2;
using SteamKit2.Internal;

namespace TrebuchetLib.Services
{
    public class Steam : IDebugListener
    {
        private readonly ILogger<Steam> _logger;
        private readonly AppSetup _appSetup;
        private readonly IProgressCallback<DepotDownloader.Progress> _progress;
        private readonly Steam3Session _session;

        public Steam(ILogger<Steam> logger, AppSetup appSetup, IProgressCallback<DepotDownloader.Progress> progress)
        {
            _logger = logger;
            _appSetup = appSetup;
            _progress = progress;

            DebugLog.AddListener(this);
            Util.ConsoleWriteRedirect += OnConsoleWriteRedirect;
            ContentDownloader.Config.RememberPassword = false;
            ContentDownloader.Config.DownloadManifestOnly = false;
            ContentDownloader.Config.LoginID = null;
            ContentDownloader.Config.Progress = progress;
            UpdateDownloaderConfig();
            _session = ContentDownloader.InitializeSteam3(null, null);
            _session.Connected += (_, _) => Connected?.Invoke(this, EventArgs.Empty);
            _session.Disconnected += (_, _) => Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? Connected;
        public event EventHandler? Disconnected;
        public bool IsConnected => _session.IsLoggedOn;

        public void ClearCache()
        {
            UpdateDownloaderConfig();
            Tools.DeleteIfExists(ContentDownloader.Config.DepotConfigDirectory);
        }

        public async Task Connect()
        {
            await _session.Connect();
        }

        public void Disconnect(bool sendLogOff = true)
        {
            _session.Disconnect(sendLogOff);
        }

        public void SetTemporaryProgress(IProgress<DepotDownloader.Progress> progress)
        {
            ContentDownloader.Config.Progress = progress;
        }

        public void RestoreProgress()
        {
            ContentDownloader.Config.Progress = _progress;
        }

        /// <summary>
        /// Count the amount of instances currently installed. This does not verify if the files are valid, just the main binary.
        /// </summary>
        /// <returns></returns>
        public int GetInstalledInstances()
        {
            int count = 0;

            string folder = Path.Combine(_appSetup.GetBaseInstancePath());
            if (!Directory.Exists(folder))
                return 0;

            string[] instances = Directory.GetDirectories(folder);
            foreach (string instance in instances)
            {
                string bin = Path.Combine(instance, Constants.FolderGameBinaries, Constants.FileServerBin);
                if (File.Exists(bin))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Get the installed local build id of a specified instance
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public ulong GetInstanceBuildId(int instance)
        {
            string manifest = Path.Combine(
                _appSetup.GetInstancePath(instance),
                Constants.FileBuildID);

            if (!File.Exists(manifest))
                return 0;

            string content = File.ReadAllText(manifest);
            if (ulong.TryParse(content, out ulong buildID))
                return buildID;
            return 0;
        }

        public void SetInstanceBuildId(int instance, ulong buildId)
        {
            string manifest = Path.Combine(
                _appSetup.GetInstancePath(instance),
                Constants.FileBuildID);
            
            File.WriteAllText(manifest, buildId.ToString());
        }
        
        public async Task<ulong> GetSteam3AppBuildNumber(uint appId)
        {
            UpdateDownloaderConfig();

            if (appId == ContentDownloader.INVALID_APP_ID)
                return 0;

            await _session.RequestAppInfo(appId, true);
            var depots = ContentDownloader.GetSteam3AppSection(appId, EAppInfoSection.Depots);
            var branches = depots["branches"];
            var node = branches[ContentDownloader.DEFAULT_BRANCH];

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
        /// <returns></returns>
        public async Task<uint> GetSteamBuildId()
        {
            UpdateDownloaderConfig();

            if (!await WaitSteamConnectionAsync())
                throw new Exception("Could not connect to steam");

            return await Task.Run(async () =>
            {
                try
                {
                    await _session.RequestAppInfo(_appSetup.ServerAppId, true);
                    return ContentDownloader.GetSteam3AppBuildNumber(_appSetup.ServerAppId,
                        ContentDownloader.DEFAULT_BRANCH);
                }
                catch(Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot retrieve build ID");
                    return 0U;
                }
            });
        }

        public async Task<CPublishedFile_QueryFiles_Response?> QueryWorkshopSearch(uint appId, string searchTerms, uint perPage,
            uint page)
        {
            var data = new Dictionary<string, object>
            {
                {@"search", searchTerms},
                {@"perPage", perPage},
                {@"page", page}
            };
            using var scope = _logger.BeginScope(data);
            _logger.LogInformation(@"Begin workshop search");
            try
            {
                return await _session.QueryPublishedFileSearch(appId, searchTerms, perPage, page);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed");
                return null;
            }
        }

        /// <summary>
        /// Compare a list of manifest ID from Published Files with the local steam cache
        /// </summary>
        /// <param name="keyValuePairs"></param>
        /// <returns></returns>
        public IEnumerable<UGCFileStatus> GetUpdatedUGCFileIDs(IEnumerable<(ulong pubId, ulong manisfestId)> keyValuePairs)
        {
            UpdateDownloaderConfig();

            var depotConfigStore = DepotConfigStore.LoadInstanceFromFile(
                Path.Combine(
                    _appSetup.GetWorkshopFolder(), 
                    ContentDownloader.CONFIG_DIR, 
                    ContentDownloader.DEPOT_CONFIG));
            foreach (var (pubID, manisfestID) in keyValuePairs)
            {
                if (!depotConfigStore.InstalledUGCManifestIDs.TryGetValue(pubID, out ulong manisfest))
                    yield return new UGCFileStatus(pubID, UGCStatus.Missing);
                if (manisfest != manisfestID)
                {
                    if(manisfest == ContentDownloader.INVALID_MANIFEST_ID)
                        yield return new UGCFileStatus(pubID, UGCStatus.Corrupted);
                    else
                        yield return new UGCFileStatus(pubID, UGCStatus.Updatable);
                }

                yield return new UGCFileStatus(pubID, UGCStatus.UpToDate);
            }
        }

        public void ClearUGCFileIdsFromStorage(IEnumerable<ulong> idList)
        {
            UpdateDownloaderConfig();
            DepotConfigStore.LoadFromFile(
                Path.Combine(
                    _appSetup.GetWorkshopFolder(), 
                    ContentDownloader.CONFIG_DIR, 
                    ContentDownloader.DEPOT_CONFIG));
            
            foreach (var id in idList)
                DepotConfigStore.Instance.InstalledUGCManifestIDs.Remove(id);
            DepotConfigStore.Save();
        }

        public ICollection<ulong> GetUGCFileIdsFromStorage()
        {
            UpdateDownloaderConfig();
            DepotConfigStore.LoadFromFile(
                Path.Combine(
                    _appSetup.GetWorkshopFolder(), 
                    ContentDownloader.CONFIG_DIR, 
                    ContentDownloader.DEPOT_CONFIG));
            return DepotConfigStore.Instance.InstalledUGCManifestIDs.Keys.ToList();
        }

        /// <summary>
        /// Remove all junctions present in any server instance folder. Used before update to avoid crash du to copy error on junction folders.
        /// </summary>
        public void RemoveAllSymbolicLinks()
        {
            string folder = _appSetup.GetBaseInstancePath();
            if (!Directory.Exists(folder))
                return;
            string[] instances = Directory.GetDirectories(folder);
            foreach (string instance in instances)
                Tools.RemoveSymboliclink(Path.Combine(instance, Constants.FolderGameSave));
        }

        public void UpdateDownloaderConfig()
        {
            ContentDownloader.Config.CellID = 0; //TODO: Offer regional download selection
            ContentDownloader.Config.MaxDownloads = _appSetup.Config.MaxDownloads;
            ContentDownloader.Config.MaxServers = Math.Max(_appSetup.Config.MaxServers, ContentDownloader.Config.MaxDownloads);
            ContentDownloader.Config.DepotConfigDirectory = Path.Combine(_appSetup.GetWorkshopFolder(), ContentDownloader.CONFIG_DIR);
            AccountSettingsStore.LoadFromFile(
                Path.Combine(_appSetup.GetWorkshopFolder(), 
                    _appSetup.VersionFolder, 
                    "account.config"));
        }

        /// <summary>
        /// Update a list of mods from the steam workshop.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task UpdateMods(IEnumerable<ulong> enumerable, CancellationTokenSource cts)
        {
            if (!await WaitSteamConnectionAsync()) return;

            UpdateDownloaderConfig();
            ContentDownloader.Config.InstallDirectory = Path.Combine(_appSetup.GetWorkshopFolder());
            await Task.Run(async () =>
            {
                try
                {
                    await ContentDownloader.DownloadUGCAsync([Constants.AppIDLiveClient, Constants.AppIDTestLiveClient],
                        enumerable, ContentDownloader.DEFAULT_BRANCH, cts);
                }
                catch(Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot update the mods");
                }
            });
        }

        /// <summary>
        /// Performs a server update on a targeted instance.
        /// </summary>
        /// <param name="instanceNumber"></param>
        /// <param name="cts"></param>
        /// <param name="reinstall"></param>
        /// <returns></returns>
        public async Task UpdateServer(int instanceNumber, CancellationTokenSource cts, bool reinstall = false)
        {
            if (_appSetup.Config.ServerInstanceCount <= 0) return;

            if (!await WaitSteamConnectionAsync().ConfigureAwait(false)) return;

            _logger.LogInformation($"Updating server instance {instanceNumber}.");

            string instance = _appSetup.GetInstancePath(instanceNumber);
            if (reinstall)
            {
                Tools.RemoveSymboliclink(Path.Combine(instance, Constants.FolderGameSave));
                Tools.DeleteIfExists(instance);
            }

            Tools.CreateDir(instance);
            UpdateDownloaderConfig();
            ContentDownloader.Config.InstallDirectory = instance;
            await Task.Run(async () =>
            {
                try
                {
                    await ContentDownloader.DownloadAppAsync(_appSetup.ServerAppId, [],
                        ContentDownloader.DEFAULT_BRANCH, null, null, null, false, false, cts);
                    var buildId = await GetSteam3AppBuildNumber(_appSetup.ServerAppId);
                    SetInstanceBuildId(instanceNumber, buildId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, @"Failed to update server");
                }
            }, cts.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Update a server instance other than 0, from the content of instance 0.
        /// </summary>
        /// <param name="instanceNumber"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public async Task UpdateServerFromInstance0(int instanceNumber, CancellationToken token)
        {
            if (_appSetup.Config.ServerInstanceCount <= 0) return;
            if (instanceNumber == 0)
                throw new Exception("Can't update instance 0 with itself.");

            _logger.LogInformation("Updating server instance {0} from instance 0.", instanceNumber);

            string instance = _appSetup.GetInstancePath(instanceNumber);
            string instance0 = _appSetup.GetInstancePath(0);

            if (!Directory.Exists(instance0))
                throw new DirectoryNotFoundException($"{instance0} was not found.");

            Tools.RemoveSymboliclink(Path.Combine(instance0, Constants.FolderGameSave));
            Tools.RemoveSymboliclink(Path.Combine(instance, Constants.FolderGameSave));
            Tools.DeleteIfExists(instance);
            Tools.CreateDir(instance);

            await Tools.DeepCopyAsync(instance0, instance, token);
        }

        /// <summary>
        /// Update instance 0 using steamcmd, then copy the files of instance0 to update other instances.
        /// </summary>
        /// <returns></returns>
        public async Task UpdateServerInstances(CancellationTokenSource cts)
        {
            if (_appSetup.Config.ServerInstanceCount <= 0) return;

            if (!await WaitSteamConnectionAsync()) return;

            _logger.LogInformation("Updating all server instances...");
            int count = _appSetup.Config.ServerInstanceCount;
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    await UpdateServer(i, cts);
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
            await _session.Connect();
            return IsConnected;
        }

        public void WriteLine(string category, string msg)
        {
            _logger.LogInformation($"[{category}] {msg}");
        }

        private void OnConsoleWriteRedirect(string obj)
        {
            obj = obj.Replace(Environment.NewLine, string.Empty);
            _logger.LogInformation($"[ContentDownloader] {obj}");
        }
    }
}