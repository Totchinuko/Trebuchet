using SteamKit2;
using SteamKit2.CDN;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Goog
{
    public class ContentDownloader
    {
        public const string DEFAULT_BRANCH = "public";
        public const uint INVALID_APP_ID = uint.MaxValue;
        public const uint INVALID_DEPOT_ID = uint.MaxValue;
        public const ulong INVALID_MANIFEST_ID = ulong.MaxValue;
        private const string DEFAULT_DOWNLOAD_DIR = "depots";
        private readonly string STAGING_DIR;
        private readonly string STEAMKIT_DIR;
        private CDNClientPool cdnPool;
        private DepotConfigStore depotConfigStore;
        private SteamSession steam;

        public ContentDownloader(SteamSession steam, Config config, uint appID)
        {
            this.steam = steam;
            cdnPool = new CDNClientPool(steam, appID);
            Config = config;

            STEAMKIT_DIR = Path.Combine(Config.InstallPath, "SteamCache");
            STAGING_DIR = Path.Combine(STEAMKIT_DIR, "Staging");
            depotConfigStore = DepotConfigStore.LoadFromFile(Path.Combine(STEAMKIT_DIR, "depot.config"));
        }

        /// <summary>
        /// Download progress for the current app. Use a lock on its reference to access it in a thread-safe manner.
        /// </summary>
        public GlobalDownloadCounter? GlobalDownloadCounter { get; private set; }

        /// <summary>
        /// Current install directory for the app. Set this before downloading.
        /// </summary>
        public string? InstallDirectory { get; private set; }

        private Config Config { get; set; }

        /// <summary>
        /// Downloads the specified app using the specified list of depots and manifests, or all accessible depots if list is empty.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="depotManifestIds"></param>
        /// <param name="branch">Since we are only using anonymous login, most likely will always be "public"</param>
        /// <param name="os">null to auto detect</param>
        /// <param name="arch">null to auto detect</param>
        /// <param name="language">null to default to english</param>
        /// <param name="lowViolence">Probably want this to false for Conan</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ContentDownloaderException"></exception>
        public async Task DownloadAppAsync(uint appId, List<(uint depotId, ulong manifestId)> depotManifestIds, string branch, string? os, string? arch, string? language, bool lowViolence, CancellationTokenSource cts)
        {
            if (string.IsNullOrWhiteSpace(InstallDirectory))
                throw new Exception("Install directory not set.");

            steam.RequestAppInfo(appId);

            if (!AccountHasAccess(appId))
            {
                if (steam.RequestFreeAppLicense(appId))
                {
                    Log.Write("Obtained FreeOnDemand license for app {0}", LogSeverity.Info, appId);

                    // Fetch app info again in case we didn't get it fully without a license.
                    steam.RequestAppInfo(appId, true);
                }
                else
                {
                    var contentName = GetAppName(appId);
                    throw new ContentDownloaderException(String.Format("App {0} ({1}) is not available from this account.", appId, contentName));
                }
            }

            var hasSpecificDepots = depotManifestIds.Count > 0;
            var depotIdsFound = new List<uint>();
            var depotIdsExpected = depotManifestIds.Select(x => x.Item1).ToList();
            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);

            Log.Write("Using app branch: '{0}'.", LogSeverity.Debug, branch);

            if (depots != null)
            {
                foreach (var depotSection in depots.Children)
                {
                    var id = INVALID_DEPOT_ID;
                    if (depotSection.Children.Count == 0)
                        continue;

                    if (!uint.TryParse(depotSection.Name, out id))
                        continue;

                    if (hasSpecificDepots && !depotIdsExpected.Contains(id))
                        continue;

                    if (!hasSpecificDepots)
                    {
                        var depotConfig = depotSection["config"];
                        if (depotConfig != KeyValue.Invalid)
                        {
                            var oss = depotConfig["oslist"].Value;
                            if (depotConfig["oslist"] != KeyValue.Invalid && !string.IsNullOrWhiteSpace(oss))
                            {
                                var oslist = oss.Split(',');
                                if (Array.IndexOf(oslist, os ?? Util.GetSteamOS()) == -1)
                                    continue;
                            }

                            if (depotConfig["osarch"] != KeyValue.Invalid && !string.IsNullOrWhiteSpace(depotConfig["osarch"].Value))
                            {
                                var depotArch = depotConfig["osarch"].Value;
                                if (depotArch != (arch ?? Util.GetSteamArch()))
                                    continue;
                            }

                            if (depotConfig["language"] != KeyValue.Invalid && !string.IsNullOrWhiteSpace(depotConfig["language"].Value))
                            {
                                var depotLang = depotConfig["language"].Value;
                                if (depotLang != (language ?? "english"))
                                    continue;
                            }

                            if (!lowViolence && depotConfig["lowviolence"] != KeyValue.Invalid && depotConfig["lowviolence"].AsBoolean())
                                continue;
                        }
                    }

                    depotIdsFound.Add(id);

                    if (!hasSpecificDepots)
                        depotManifestIds.Add((id, INVALID_MANIFEST_ID));
                }
            }

            if (depotManifestIds.Count == 0 && !hasSpecificDepots)
            {
                throw new ContentDownloaderException(String.Format("Couldn't find any depots to download for app {0}", appId));
            }

            if (depotIdsFound.Count < depotIdsExpected.Count)
            {
                var remainingDepotIds = depotIdsExpected.Except(depotIdsFound);
                throw new ContentDownloaderException(String.Format("Depot {0} not listed for app {1}", string.Join(", ", remainingDepotIds), appId));
            }

            var infos = new List<DepotDownloadInfo>();

            if (!CreateDirectories(out var installDir))
                throw new Exception("Unable to create install directories!");

            foreach (var depotManifest in depotManifestIds)
            {
                if (TryGetDepotInfo(depotManifest.Item1, appId, depotManifest.Item2, branch, installDir, out var info))
                    infos.Add(info);
            }

            try
            {
                await DownloadSteam3Async(appId, infos, cts).ConfigureAwait(false);
                WriteBuildIDFile(appId, branch);
            }
            catch (OperationCanceledException)
            {
                Log.Write("App {0} was not completely downloaded.", LogSeverity.Warning, appId);
                throw;
            }
        }

        /// <summary>
        /// Download a single workshop item.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="publishedFileId"></param>
        /// <returns></returns>
        public async Task DownloadPubfileAsync(uint appId, ulong publishedFileId, CancellationTokenSource cts)
        {
            var details = steam.GetPublishedFileDetails(appId, publishedFileId);

            if (!string.IsNullOrEmpty(details?.file_url))
            {
                await DownloadWebFile(appId, details.filename, details.file_url);
            }
            else if (details?.hcontent_file > 0)
            {
                await DownloadUGCAsync(appId, new List<ulong> { publishedFileId }, DEFAULT_BRANCH, cts);
            }
            else
            {
                Log.Write("Unable to locate manifest ID for published file {0}", LogSeverity.Error, publishedFileId);
            }
        }

        /// <summary>
        /// Download a list of manifests handled UGC files.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="publishedFiles"></param>
        /// <param name="branch"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ContentDownloaderException"></exception>
        public async Task DownloadUGCAsync(uint appId, IEnumerable<ulong> publishedFiles, string branch, CancellationTokenSource cts)
        {
            if (string.IsNullOrWhiteSpace(InstallDirectory))
                throw new Exception("Install directory not set.");

            steam.RequestAppInfo(appId);

            if (!AccountHasAccess(appId))
            {
                if (steam.RequestFreeAppLicense(appId))
                {
                    Log.Write("Obtained FreeOnDemand license for app {0}", LogSeverity.Info, appId);

                    // Fetch app info again in case we didn't get it fully without a license.
                    steam.RequestAppInfo(appId, true);
                }
                else
                {
                    var contentName = GetAppName(appId);
                    throw new ContentDownloaderException(String.Format("App {0} ({1}) is not available from this account.", appId, contentName));
                }
            }

            var list = steam.GetPublishedFileDetails(appId, publishedFiles);
            var depotManifestIds = list.Where(x => x.hcontent_file > 0).Select(i => (appId, i.publishedfileid, i.hcontent_file)).ToList();

            var hasSpecificDepots = depotManifestIds.Count > 0;
            var depotIdsFound = new List<uint>();
            var depotIdsExpected = depotManifestIds.Select(x => x.appId).ToList();
            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);

            var workshopDepot = depots["workshopdepot"].AsUnsignedInteger();
            if (workshopDepot != 0 && !depotIdsExpected.Contains(workshopDepot))
            {
                depotIdsExpected.Add(workshopDepot);
                depotManifestIds = depotManifestIds.Select(i => (workshopDepot, i.publishedfileid, i.hcontent_file)).ToList();
            }

            depotIdsFound.AddRange(depotIdsExpected);

            if (depotManifestIds.Count == 0 && !hasSpecificDepots)
            {
                throw new ContentDownloaderException(String.Format("Couldn't find any depots to download for app {0}", appId));
            }

            if (depotIdsFound.Count < depotIdsExpected.Count)
            {
                var remainingDepotIds = depotIdsExpected.Except(depotIdsFound);
                throw new ContentDownloaderException(String.Format("Depot {0} not listed for app {1}", string.Join(", ", remainingDepotIds), appId));
            }

            var infos = new List<DepotDownloadInfo>();

            foreach (var depotManifest in depotManifestIds)
            {
                if (!CreateUGCDirectories(appId, depotManifest.publishedfileid, out var installDir))
                    throw new Exception("Unable to create install directories!");
                if (TryGetDepotInfo(depotManifest.appId, appId, depotManifest.hcontent_file, branch, installDir, out var info))
                    infos.Add(info);
            }

            try
            {
                await DownloadSteam3Async(appId, infos, cts).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Log.Write("App {0} was not completely downloaded.", LogSeverity.Warning, appId);
                throw;
            }
        }

        /// <summary>
        /// Get current build number for a specific branch of an app.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="branch"></param>
        /// <returns></returns>
        public uint GetSteam3AppBuildNumber(uint appId, string branch)
        {
            if (appId == INVALID_APP_ID)
                return 0;

            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);
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
        /// The target install directory for downloaded content.
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void SetInstallDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                InstallDirectory = path;
            }
            else
            {
                throw new DirectoryNotFoundException("Directory does not exist");
            }
        }

        /// <summary>
        /// Shutdown and dispose resources.
        /// </summary>
        public void Shutdown()
        {
            cdnPool.Shutdown();
        }

        internal KeyValue GetSteam3AppSection(uint appId, EAppInfoSection section)
        {
            if (!steam.AppInfo.TryGetValue(appId, out var app) || app == null)
                return KeyValue.Invalid;

            var appinfo = app.KeyValues;
            string section_key;

            switch (section)
            {
                case EAppInfoSection.Common:
                    section_key = "common";
                    break;

                case EAppInfoSection.Extended:
                    section_key = "extended";
                    break;

                case EAppInfoSection.Config:
                    section_key = "config";
                    break;

                case EAppInfoSection.Depots:
                    section_key = "depots";
                    break;

                default:
                    throw new NotImplementedException();
            }

            var section_kv = appinfo.Children.Where(c => c.Name == section_key).FirstOrDefault() ?? KeyValue.Invalid;
            return section_kv;
        }

        private bool AccountHasAccess(uint depotId)
        {
            if (steam == null || steam.User.SteamID == null || steam.User.SteamID.AccountType != EAccountType.AnonUser)
                return false;

            // We only work with annon accounts
            IEnumerable<uint> licenseQuery;
            licenseQuery = new List<uint> { 17906 };

            steam.RequestPackageInfo(licenseQuery);

            foreach (var license in licenseQuery)
            {
                if (steam.PackageInfo.TryGetValue(license, out var package) && package != null)
                {
                    if (package.KeyValues["appids"].Children.Any(child => child.AsUnsignedInteger() == depotId))
                        return true;

                    if (package.KeyValues["depotids"].Children.Any(child => child.AsUnsignedInteger() == depotId))
                        return true;
                }
            }

            return false;
        }

        private bool CreateDirectories([NotNullWhen(true)] out string? installDir)
        {
            if (string.IsNullOrWhiteSpace(InstallDirectory))
                throw new ArgumentNullException(nameof(InstallDirectory));

            installDir = null;
            try
            {
                installDir = InstallDirectory;
                Directory.CreateDirectory(installDir);
                Directory.CreateDirectory(Path.Combine(installDir, STAGING_DIR));
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool CreateUGCDirectories(uint appid, ulong publishedFileId, [NotNullWhen(true)] out string? installDir)
        {
            if (string.IsNullOrWhiteSpace(InstallDirectory))
                throw new ArgumentNullException(nameof(InstallDirectory));

            installDir = null;
            try
            {
                installDir = Path.Combine(InstallDirectory, appid.ToString(), publishedFileId.ToString());
                Directory.CreateDirectory(installDir);
                Directory.CreateDirectory(Path.Combine(installDir, STAGING_DIR));
            }
            catch
            {
                return false;
            }

            return true;
        }

        private async Task DownloadSteam3Async(uint appId, List<DepotDownloadInfo> depots, CancellationTokenSource cts)
        {
            if (string.IsNullOrWhiteSpace(InstallDirectory))
                throw new ArgumentNullException(nameof(InstallDirectory));

            cdnPool.ExhaustedToken = cts;

            GlobalDownloadCounter = new GlobalDownloadCounter();
            var depotsToDownload = new List<DepotFilesData>(depots.Count);
            var allFileNamesAllDepots = new HashSet<String>();

            // First, fetch all the manifests for each depot (including previous manifests) and perform the initial setup
            foreach (var depot in depots)
            {
                var depotFileData = await ProcessDepotManifestAndFiles(cts, appId, depot);

                if (depotFileData != null)
                {
                    depotsToDownload.Add(depotFileData);
                    allFileNamesAllDepots.UnionWith(depotFileData.allFileNames);
                }

                cts.Token.ThrowIfCancellationRequested();
            }

            // If we're about to write all the files to the same directory, we will need to first de-duplicate any files by path
            // This is in last-depot-wins order, from Steam or the list of depots supplied by the user
            if (depotsToDownload.Count > 0)
            {
                var claimedFileNames = new HashSet<String>();

                for (var i = depotsToDownload.Count - 1; i >= 0; i--)
                {
                    // For each depot, remove all files from the list that have been claimed by a later depot
                    depotsToDownload[i].filteredFiles.RemoveAll(file => claimedFileNames.Contains(file.FileName));

                    claimedFileNames.UnionWith(depotsToDownload[i].allFileNames);
                }
            }

            foreach (var depotFileData in depotsToDownload)
            {
                await DownloadSteam3AsyncDepotFiles(cts, appId, GlobalDownloadCounter, depotFileData, allFileNamesAllDepots);
            }

            Log.Write("Total downloaded: {0} bytes ({1} bytes uncompressed) from {2} depots", LogSeverity.Info,
                GlobalDownloadCounter.TotalBytesCompressed, GlobalDownloadCounter.TotalBytesUncompressed, depots.Count);
        }

        private void DownloadSteam3AsyncDepotFile(
            CancellationTokenSource cts,
            DepotFilesData depotFilesData,
            ProtoManifest.FileData file,
            ConcurrentQueue<(FileStreamData, ProtoManifest.FileData, ProtoManifest.ChunkData)> networkChunkQueue)
        {
            cts.Token.ThrowIfCancellationRequested();

            var depot = depotFilesData.depotDownloadInfo;
            var stagingDir = depotFilesData.stagingDir;
            var depotDownloadCounter = depotFilesData.depotCounter;
            var oldProtoManifest = depotFilesData.previousManifest;
            ProtoManifest.FileData? oldManifestFile = null;
            if (oldProtoManifest != null)
            {
                oldManifestFile = oldProtoManifest.Files.SingleOrDefault(f => f.FileName == file.FileName);
            }

            var fileFinalPath = Path.Combine(depot.installDir, file.FileName);
            var fileStagingPath = Path.Combine(stagingDir, file.FileName);

            // This may still exist if the previous run exited before cleanup
            if (File.Exists(fileStagingPath))
            {
                File.Delete(fileStagingPath);
            }

            List<ProtoManifest.ChunkData> neededChunks;
            var fi = new FileInfo(fileFinalPath);
            var fileDidExist = fi.Exists;
            if (!fileDidExist)
            {
                Log.Write("Pre-allocating {0}", LogSeverity.Debug, fileFinalPath);

                // create new file. need all chunks
                using var fs = File.Create(fileFinalPath);
                try
                {
                    fs.SetLength((long)file.TotalSize);
                }
                catch (IOException ex)
                {
                    throw new ContentDownloaderException(String.Format("Failed to allocate file {0}: {1}", fileFinalPath, ex.Message));
                }

                neededChunks = new List<ProtoManifest.ChunkData>(file.Chunks);
            }
            else
            {
                // open existing
                if (oldManifestFile != null)
                {
                    neededChunks = new List<ProtoManifest.ChunkData>();

                    var hashMatches = oldManifestFile.FileHash.SequenceEqual(file.FileHash);
                    if (Config.VerifyAll || !hashMatches)
                    {
                        // we have a version of this file, but it doesn't fully match what we want
                        if (Config.VerifyAll)
                        {
                            Log.Write("Validating {0}", LogSeverity.Debug, fileFinalPath);
                        }

                        var matchingChunks = new List<ChunkMatch>();

                        foreach (var chunk in file.Chunks)
                        {
                            var oldChunk = oldManifestFile.Chunks.FirstOrDefault(c => c.ChunkID?.SequenceEqual(chunk.ChunkID ?? new byte[0]) ?? false);
                            if (oldChunk != null)
                            {
                                matchingChunks.Add(new ChunkMatch(oldChunk, chunk));
                            }
                            else
                            {
                                neededChunks.Add(chunk);
                            }
                        }

                        var orderedChunks = matchingChunks.OrderBy(x => x.OldChunk.Offset);

                        var copyChunks = new List<ChunkMatch>();

                        using (var fsOld = File.Open(fileFinalPath, FileMode.Open))
                        {
                            foreach (var match in orderedChunks)
                            {
                                fsOld.Seek((long)match.OldChunk.Offset, SeekOrigin.Begin);

                                var tmp = new byte[match.OldChunk.UncompressedLength];
                                fsOld.Read(tmp, 0, tmp.Length);

                                var adler = Util.AdlerHash(tmp);
                                if (match.OldChunk.Checksum != null && !adler.SequenceEqual(match.OldChunk.Checksum))
                                {
                                    neededChunks.Add(match.NewChunk);
                                }
                                else
                                {
                                    copyChunks.Add(match);
                                }
                            }
                        }

                        if (!hashMatches || neededChunks.Count > 0)
                        {
                            File.Move(fileFinalPath, fileStagingPath);

                            using (var fsOld = File.Open(fileStagingPath, FileMode.Open))
                            {
                                using var fs = File.Open(fileFinalPath, FileMode.Create);
                                try
                                {
                                    fs.SetLength((long)file.TotalSize);
                                }
                                catch (IOException ex)
                                {
                                    throw new ContentDownloaderException(String.Format("Failed to resize file to expected size {0}: {1}", fileFinalPath, ex.Message));
                                }

                                foreach (var match in copyChunks)
                                {
                                    fsOld.Seek((long)match.OldChunk.Offset, SeekOrigin.Begin);

                                    var tmp = new byte[match.OldChunk.UncompressedLength];
                                    fsOld.Read(tmp, 0, tmp.Length);

                                    fs.Seek((long)match.NewChunk.Offset, SeekOrigin.Begin);
                                    fs.Write(tmp, 0, tmp.Length);
                                }
                            }

                            File.Delete(fileStagingPath);
                        }
                    }
                }
                else
                {
                    // No old manifest or file not in old manifest. We must validate.

                    using var fs = File.Open(fileFinalPath, FileMode.Open);
                    if ((ulong)fi.Length != file.TotalSize)
                    {
                        try
                        {
                            fs.SetLength((long)file.TotalSize);
                        }
                        catch (IOException ex)
                        {
                            throw new ContentDownloaderException(String.Format("Failed to allocate file {0}: {1}", fileFinalPath, ex.Message));
                        }
                    }

                    Log.Write("Validating {0}", LogSeverity.Debug, fileFinalPath);
                    neededChunks = Util.ValidateSteam3FileChecksums(fs, file.Chunks.OrderBy(x => x.Offset).ToArray());
                }

                if (neededChunks.Count() == 0)
                {
                    lock (depotDownloadCounter)
                    {
                        depotDownloadCounter.SizeDownloaded += file.TotalSize;
                        Log.Write("{0,6:#00.00}% {1}", LogSeverity.Debug, (depotDownloadCounter.SizeDownloaded / (float)depotDownloadCounter.CompleteDownloadSize) * 100.0f, fileFinalPath);
                    }

                    return;
                }

                var sizeOnDisk = (file.TotalSize - (ulong)neededChunks.Select(x => (long)x.UncompressedLength).Sum());
                lock (depotDownloadCounter)
                {
                    depotDownloadCounter.SizeDownloaded += sizeOnDisk;
                }
            }

            var fileIsExecutable = file.Flags.HasFlag(EDepotFileFlag.Executable);
            if (fileIsExecutable && (!fileDidExist || oldManifestFile == null || !oldManifestFile.Flags.HasFlag(EDepotFileFlag.Executable)))
            {
                PlatformUtilities.SetExecutable(fileFinalPath, true);
            }
            else if (!fileIsExecutable && oldManifestFile != null && oldManifestFile.Flags.HasFlag(EDepotFileFlag.Executable))
            {
                PlatformUtilities.SetExecutable(fileFinalPath, false);
            }

            var fileStreamData = new FileStreamData(new SemaphoreSlim(1), neededChunks.Count);

            foreach (var chunk in neededChunks)
            {
                networkChunkQueue.Enqueue((fileStreamData, file, chunk));
            }
        }

        private async Task DownloadSteam3AsyncDepotFileChunk(CancellationTokenSource cts, uint appId, GlobalDownloadCounter downloadCounter, DepotFilesData depotFilesData, ProtoManifest.FileData file, FileStreamData fileStreamData, ProtoManifest.ChunkData chunk)
        {
            if (cdnPool == null)
                throw new Exception("cdnPool is not available.");
            cts.Token.ThrowIfCancellationRequested();

            var depot = depotFilesData.depotDownloadInfo;
            var depotDownloadCounter = depotFilesData.depotCounter;

            var chunkID = Util.EncodeHexString(chunk.ChunkID ?? new byte[0]);

            var data = new DepotManifest.ChunkData();
            data.ChunkID = chunk.ChunkID;
            data.Checksum = chunk.Checksum;
            data.Offset = chunk.Offset;
            data.CompressedLength = chunk.CompressedLength;
            data.UncompressedLength = chunk.UncompressedLength;

            DepotChunk? chunkData = null;

            do
            {
                cts.Token.ThrowIfCancellationRequested();

                Server? connection = null;

                try
                {
                    connection = cdnPool.GetConnection(cts.Token);

                    DebugLog.WriteLine("ContentDownloader", "Downloading chunk {0} from {1} with {2}", chunkID, connection, cdnPool.ProxyServer != null ? cdnPool.ProxyServer : "no proxy");
                    chunkData = await cdnPool.CDNClient.DownloadDepotChunkAsync(
                        depot.id,
                        data,
                        connection,
                        depot.depotKey,
                        cdnPool.ProxyServer).ConfigureAwait(false);

                    cdnPool.ReturnConnection(connection);
                }
                catch (TaskCanceledException)
                {
                    Log.Write("Connection timeout downloading chunk {0}", LogSeverity.Warning, chunkID);
                }
                catch (SteamKitWebRequestException e)
                {
                    cdnPool.ReturnBrokenConnection(connection);

                    if (e.StatusCode == HttpStatusCode.Unauthorized || e.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Log.Write("Encountered 401 for chunk {0}. Aborting.", LogSeverity.Error, chunkID);
                        break;
                    }

                    Log.Write("Encountered error downloading chunk {0}: {1}", LogSeverity.Error, chunkID, e.StatusCode);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    cdnPool.ReturnBrokenConnection(connection);
                    Log.Write("Encountered unexpected error downloading chunk {0}: {1}", LogSeverity.Error, chunkID, e.Message);
                }
            } while (chunkData == null);

            if (chunkData == null)
            {
                cts.Cancel();
                throw new Exception(string.Format("Failed to find any server with chunk {0} for depot {1}. Aborting.", chunkID, depot.id));
            }

            try
            {
                await fileStreamData.fileLock.WaitAsync().ConfigureAwait(false);

                if (fileStreamData.fileStream == null)
                {
                    var fileFinalPath = Path.Combine(depot.installDir, file.FileName);
                    fileStreamData.fileStream = File.Open(fileFinalPath, FileMode.Open);
                }

                fileStreamData.fileStream.Seek((long)chunkData.ChunkInfo.Offset, SeekOrigin.Begin);
                await fileStreamData.fileStream.WriteAsync(chunkData.Data, 0, chunkData.Data.Length);
            }
            finally
            {
                fileStreamData.fileLock.Release();
            }

            var remainingChunks = Interlocked.Decrement(ref fileStreamData.chunksToDownload);
            if (remainingChunks == 0)
            {
                fileStreamData.fileStream?.Dispose();
                fileStreamData.fileLock.Dispose();
            }

            ulong sizeDownloaded = 0;
            lock (depotDownloadCounter)
            {
                sizeDownloaded = depotDownloadCounter.SizeDownloaded + (ulong)chunkData.Data.Length;
                depotDownloadCounter.SizeDownloaded = sizeDownloaded;
                depotDownloadCounter.DepotBytesCompressed += chunk.CompressedLength;
                depotDownloadCounter.DepotBytesUncompressed += chunk.UncompressedLength;
            }

            lock (downloadCounter)
            {
                downloadCounter.TotalBytesCompressed += chunk.CompressedLength;
                downloadCounter.TotalBytesUncompressed += chunk.UncompressedLength;
            }

            if (remainingChunks == 0)
            {
                var fileFinalPath = Path.Combine(depot.installDir, file.FileName);
                Log.Write("{0,6:#00.00}% {1}", LogSeverity.Debug, (sizeDownloaded / (float)depotDownloadCounter.CompleteDownloadSize) * 100.0f, fileFinalPath);
            }
        }

        private async Task DownloadSteam3AsyncDepotFiles(CancellationTokenSource cts, uint appId, GlobalDownloadCounter downloadCounter, DepotFilesData depotFilesData, HashSet<String> allFileNamesAllDepots)
        {
            if (string.IsNullOrWhiteSpace(InstallDirectory))
                throw new Exception("InstallDirectory was not set");

            var depot = depotFilesData.depotDownloadInfo;
            var depotCounter = depotFilesData.depotCounter;

            Log.Write("Downloading files from depot {0}", LogSeverity.Info, depot.id);

            var files = depotFilesData.filteredFiles.Where(f => !f.Flags.HasFlag(EDepotFileFlag.Directory)).ToArray();
            var networkChunkQueue = new ConcurrentQueue<(FileStreamData fileStreamData, ProtoManifest.FileData fileData, ProtoManifest.ChunkData chunk)>();

            await Util.InvokeAsync(
                files.Select(file => new Func<Task>(async () =>
                    await Task.Run(() => DownloadSteam3AsyncDepotFile(cts, depotFilesData, file, networkChunkQueue)))),
                maxDegreeOfParallelism: Config.MaxDownloads
            );

            await Util.InvokeAsync(
                networkChunkQueue.Select(q => new Func<Task>(async () =>
                    await Task.Run(() => DownloadSteam3AsyncDepotFileChunk(cts, appId, downloadCounter, depotFilesData,
                        q.fileData, q.fileStreamData, q.chunk)))),
                maxDegreeOfParallelism: Config.MaxDownloads
            );

            // Check for deleted files if updating the depot.
            if (depotFilesData.previousManifest != null)
            {
                var previousFilteredFiles = depotFilesData.previousManifest.Files.AsParallel().Select(f => f.FileName).ToHashSet();

                previousFilteredFiles.ExceptWith(allFileNamesAllDepots);

                foreach (var existingFileName in previousFilteredFiles)
                {
                    var fileFinalPath = Path.Combine(depot.installDir, existingFileName);

                    if (!File.Exists(fileFinalPath))
                        continue;

                    File.Delete(fileFinalPath);
                    Log.Write("Deleted {0}", LogSeverity.Info, fileFinalPath);
                }
            }

            depotConfigStore.InstalledManifestIDs[depot.id] = depot.manifestId;
            depotConfigStore.Save();

            Log.Write("Depot {0} - Downloaded {1} bytes ({2} bytes uncompressed)", LogSeverity.Info, depot.id, depotCounter.DepotBytesCompressed, depotCounter.DepotBytesUncompressed);
        }

        private async Task DownloadWebFile(uint appId, string fileName, string url)
        {
            if (!CreateDirectories(out var installDir))
            {
                Log.Write("Unable to create install directories!", LogSeverity.Critical);
                return;
            }

            var stagingDir = Path.Combine(installDir, STAGING_DIR);
            var fileStagingPath = Path.Combine(stagingDir, fileName);
            var fileFinalPath = Path.Combine(installDir, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(fileFinalPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(fileStagingPath)!);

            using (var file = File.OpenWrite(fileStagingPath))
            using (var client = HttpClientFactory.CreateHttpClient())
            {
                Log.Write("Downloading {0}", LogSeverity.Info, fileName);
                var responseStream = await client.GetStreamAsync(url);
                await responseStream.CopyToAsync(file);
            }

            if (File.Exists(fileFinalPath))
            {
                File.Delete(fileFinalPath);
            }

            File.Move(fileStagingPath, fileFinalPath);
        }

        private string GetAppName(uint appId)
        {
            var info = GetSteam3AppSection(appId, EAppInfoSection.Common);
            if (info == null)
                return string.Empty;

            return info["name"].AsString() ?? string.Empty;
        }

        private ulong GetSteam3DepotManifest(uint depotId, uint appId, string branch)
        {
            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);
            var depotChild = depots[depotId.ToString()];

            if (depotChild == KeyValue.Invalid || depotChild["manifests"] == KeyValue.Invalid)
                return INVALID_MANIFEST_ID;

            var manifests = depotChild["manifests"];

            if (manifests.Children.Count == 0)
                return INVALID_MANIFEST_ID;

            var node = manifests[branch]["gid"];

            if (node.Value == null)
                return INVALID_MANIFEST_ID;

            return UInt64.Parse(node.Value);
        }

        private async Task<DepotFilesData> ProcessDepotManifestAndFiles(CancellationTokenSource cts, uint appId, DepotDownloadInfo depot)
        {
            var depotCounter = new DepotDownloadCounter();

            Log.Write("Processing depot {0}", LogSeverity.Debug, depot.id);

            ProtoManifest? oldProtoManifest = null;
            ProtoManifest? newProtoManifest = null;
            var configDir = Path.Combine(depot.installDir, STEAMKIT_DIR);

            var lastManifestId = INVALID_MANIFEST_ID;
            depotConfigStore.InstalledManifestIDs.TryGetValue(depot.id, out lastManifestId);

            // In case we have an early exit, this will force equiv of verifyall next run.
            depotConfigStore.InstalledManifestIDs[depot.id] = INVALID_MANIFEST_ID;
            depotConfigStore.Save();

            if (lastManifestId != INVALID_MANIFEST_ID)
            {
                var oldManifestFileName = Path.Combine(configDir, string.Format("{0}_{1}.bin", depot.id, lastManifestId));

                if (File.Exists(oldManifestFileName))
                {
                    byte[] expectedChecksum, currentChecksum;

                    try
                    {
                        expectedChecksum = File.ReadAllBytes(oldManifestFileName + ".sha");
                    }
                    catch (IOException)
                    {
                        expectedChecksum = new byte[0];
                    }

                    oldProtoManifest = ProtoManifest.LoadFromFile(oldManifestFileName, out currentChecksum);

                    if (expectedChecksum == null || !expectedChecksum.SequenceEqual(currentChecksum))
                    {
                        // We only have to show this warning if the old manifest ID was different
                        if (lastManifestId != depot.manifestId)
                            Log.Write("Manifest {0} on disk did not match the expected checksum.", LogSeverity.Debug, lastManifestId);
                        oldProtoManifest = null;
                    }
                }
            }

            if (lastManifestId == depot.manifestId && oldProtoManifest != null)
            {
                newProtoManifest = oldProtoManifest;
                Log.Write("Already have manifest {0} for depot {1}.", LogSeverity.Debug, depot.manifestId, depot.id);
            }
            else
            {
                var newManifestFileName = Path.Combine(configDir, string.Format("{0}_{1}.bin", depot.id, depot.manifestId));
                if (newManifestFileName != null)
                {
                    byte[] expectedChecksum, currentChecksum;

                    try
                    {
                        expectedChecksum = File.ReadAllBytes(newManifestFileName + ".sha");
                    }
                    catch (IOException)
                    {
                        expectedChecksum = new byte[0];
                    }

                    newProtoManifest = ProtoManifest.LoadFromFile(newManifestFileName, out currentChecksum);

                    if (expectedChecksum == null || !expectedChecksum.SequenceEqual(currentChecksum))
                    {
                        Log.Write("Manifest {0} on disk did not match the expected checksum.", LogSeverity.Debug, depot.manifestId);
                        newProtoManifest = null;
                    }
                }

                if (newProtoManifest != null)
                {
                    Log.Write("Already have manifest {0} for depot {1}.", LogSeverity.Debug, depot.manifestId, depot.id);
                }
                else
                {
                    Log.Write("Downloading depot manifest...", LogSeverity.Debug);

                    DepotManifest? depotManifest = null;
                    ulong manifestRequestCode = 0;
                    var manifestRequestCodeExpiration = DateTime.MinValue;

                    do
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        Server? connection = null;

                        try
                        {
                            connection = cdnPool.GetConnection(cts.Token);

                            var now = DateTime.Now;

                            // In order to download this manifest, we need the current manifest request code
                            // The manifest request code is only valid for a specific period in time
                            if (manifestRequestCode == 0 || now >= manifestRequestCodeExpiration)
                            {
                                manifestRequestCode = await steam.GetDepotManifestRequestCodeAsync(
                                    depot.id,
                                    depot.appId,
                                    depot.manifestId,
                                    depot.branch);
                                // This code will hopefully be valid for one period following the issuing period
                                manifestRequestCodeExpiration = now.Add(TimeSpan.FromMinutes(5));

                                // If we could not get the manifest code, this is a fatal error
                                if (manifestRequestCode == 0)
                                {
                                    Log.Write("No manifest request code was returned for {0} {1}", LogSeverity.Error, depot.id, depot.manifestId);
                                    cts.Cancel();
                                }
                            }

                            Log.Write("Downloading manifest {0} from {1} with {2}", LogSeverity.Debug, depot.manifestId, connection, cdnPool.ProxyServer != null ? cdnPool.ProxyServer : "no proxy");
                            depotManifest = await cdnPool.CDNClient.DownloadManifestAsync(
                                depot.id,
                                depot.manifestId,
                                manifestRequestCode,
                                connection,
                                depot.depotKey,
                                cdnPool.ProxyServer).ConfigureAwait(false);

                            cdnPool.ReturnConnection(connection);
                        }
                        catch (TaskCanceledException)
                        {
                            Log.Write("Connection timeout downloading depot manifest {0} {1}. Retrying.", LogSeverity.Warning, depot.id, depot.manifestId);
                        }
                        catch (SteamKitWebRequestException e)
                        {
                            cdnPool.ReturnBrokenConnection(connection);

                            if (e.StatusCode == HttpStatusCode.Unauthorized || e.StatusCode == HttpStatusCode.Forbidden)
                            {
                                Log.Write("Encountered 401 for depot manifest {0} {1}. Aborting.", LogSeverity.Error, depot.id, depot.manifestId);
                                break;
                            }

                            if (e.StatusCode == HttpStatusCode.NotFound)
                            {
                                Log.Write("Encountered 404 for depot manifest {0} {1}. Aborting.", LogSeverity.Error, depot.id, depot.manifestId);
                                break;
                            }

                            Log.Write("Encountered error downloading depot manifest {0} {1}: {2}", LogSeverity.Error, depot.id, depot.manifestId, e.StatusCode);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception e)
                        {
                            cdnPool.ReturnBrokenConnection(connection);
                            Log.Write("Encountered error downloading manifest for depot {0} {1}: {2}", LogSeverity.Error, depot.id, depot.manifestId, e.Message);
                        }
                    } while (depotManifest == null);

                    if (depotManifest == null)
                    {
                        cts.Cancel();
                        throw new Exception(string.Format("\nUnable to download manifest {0} for depot {1}", depot.manifestId, depot.id));
                    }

                    // Throw the cancellation exception if requested so that this task is marked failed
                    cts.Token.ThrowIfCancellationRequested();

                    byte[] checksum;

                    newProtoManifest = new ProtoManifest(depotManifest, depot.manifestId);
                    if (newManifestFileName != null)
                    {
                        newProtoManifest.SaveToFile(newManifestFileName, out checksum);
                        File.WriteAllBytes(newManifestFileName + ".sha", checksum);
                    }

                    Log.Write(" Done!", LogSeverity.Debug);
                }
            }

            newProtoManifest.Files.Sort((x, y) => string.Compare(x.FileName, y.FileName, StringComparison.Ordinal));

            Log.Write("Manifest {0} ({1})", LogSeverity.Debug, depot.manifestId, newProtoManifest.CreationTime);

            var stagingDir = Path.Combine(depot.installDir, STAGING_DIR);

            var allFileNames = new HashSet<string>(newProtoManifest.Files.Count);

            // Pre-process
            newProtoManifest.Files.ForEach(file =>
            {
                allFileNames.Add(file.FileName);

                var fileFinalPath = Path.Combine(depot.installDir, file.FileName);
                var fileStagingPath = Path.Combine(stagingDir, file.FileName);

                if (file.Flags.HasFlag(EDepotFileFlag.Directory))
                {
                    Directory.CreateDirectory(fileFinalPath);
                    Directory.CreateDirectory(fileStagingPath);
                }
                else
                {
                    // Some manifests don't explicitly include all necessary directories
                    Directory.CreateDirectory(Path.GetDirectoryName(fileFinalPath)!);
                    Directory.CreateDirectory(Path.GetDirectoryName(fileStagingPath)!);

                    depotCounter.CompleteDownloadSize += file.TotalSize;
                }
            });

            return new DepotFilesData(depot, depotCounter)
            {
                depotDownloadInfo = depot,
                depotCounter = depotCounter,
                stagingDir = stagingDir,
                manifest = newProtoManifest,
                previousManifest = oldProtoManifest,
                filteredFiles = newProtoManifest.Files,
                allFileNames = allFileNames
            };
        }

        private bool TryGetDepotInfo(uint depotId, uint appId, ulong manifestId, string branch, string installDir, [NotNullWhen(true)] out DepotDownloadInfo? info)
        {
            if (appId != INVALID_APP_ID)
                steam.RequestAppInfo(appId);

            info = null;
            if (!AccountHasAccess(depotId))
            {
                Log.Write("Depot not available from this account.", LogSeverity.Debug);
                return false;
            }

            if (manifestId == INVALID_MANIFEST_ID)
            {
                manifestId = GetSteam3DepotManifest(depotId, appId, branch);
                if (manifestId == INVALID_MANIFEST_ID && !string.Equals(branch, DEFAULT_BRANCH, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Write("Depot {0} does not have branch named \"{1}\". Trying {2} branch.", LogSeverity.Warning, depotId, branch, DEFAULT_BRANCH);
                    branch = DEFAULT_BRANCH;
                    manifestId = GetSteam3DepotManifest(depotId, appId, branch);
                }

                if (manifestId == INVALID_MANIFEST_ID)
                {
                    Log.Write("Depot {0} missing public subsection or manifest section.", LogSeverity.Debug, depotId);
                    return false;
                }
            }

            steam.RequestDepotKey(depotId, appId);
            if (!steam.DepotKeys.ContainsKey(depotId))
            {
                Log.Write("No valid depot key for {0}, unable to download.", LogSeverity.Debug, depotId);
                return false;
            }

            var depotKey = steam.DepotKeys[depotId];

            info = new DepotDownloadInfo(depotId, appId, manifestId, branch, installDir, depotKey);
            return true;
        }

        private void WriteBuildIDFile(uint appId, string branch)
        {
            if (string.IsNullOrWhiteSpace(InstallDirectory))
                throw new ArgumentException(nameof(InstallDirectory));

            string path = Path.Combine(InstallDirectory, Config.FileBuildID);
            File.WriteAllText(path, GetSteam3AppBuildNumber(appId, branch).ToString());
        }
    }

    public class ContentDownloaderException : Exception
    {
        public ContentDownloaderException(String value) : base(value)
        {
        }
    }
}