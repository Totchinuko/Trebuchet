using SteamKit2;
using SteamKit2.Internal;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Goog
{
    public partial class SteamSession
    {
        private static readonly TimeSpan STEAM3_TIMEOUT = TimeSpan.FromSeconds(30);
        private readonly object steamLock = new object();
        private readonly SteamUnifiedMessages.UnifiedService<IPublishedFile> steamPublishedFile;
        private DateTime connectTime;
        private int currentSessionIndex;

        public delegate bool WaitCondition();

        public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo?> AppInfo { get; private set; } = new Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo?>();

        public Dictionary<uint, ulong> AppTokens { get; private set; } = new Dictionary<uint, ulong>();

        public ConcurrentDictionary<string, TaskCompletionSource<SteamApps.CDNAuthTokenCallback>> CDNAuthTokens { get; private set; } = new ConcurrentDictionary<string, TaskCompletionSource<SteamApps.CDNAuthTokenCallback>>();

        public Dictionary<uint, byte[]> DepotKeys { get; private set; } = new Dictionary<uint, byte[]>();

        public ReadOnlyCollection<SteamApps.LicenseListCallback.License> Licenses { get; private set; } = new ReadOnlyCollection<SteamApps.LicenseListCallback.License>(new List<SteamApps.LicenseListCallback.License>());

        public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo?> PackageInfo { get; private set; } = new Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo?>();

        public Dictionary<uint, ulong> PackageTokens { get; private set; } = new Dictionary<uint, ulong>();

        public async Task<ulong> GetDepotManifestRequestCodeAsync(uint depotId, uint appId, ulong manifestId, string branch)
        {
            if (!_isRunning)
                return 0;

            var requestCode = await Content.GetManifestRequestCode(depotId, appId, manifestId, branch);

            Log.Write("Got manifest request code for {0} {1} result: {2}", LogSeverity.Debug,
                depotId, manifestId,
                requestCode);

            return requestCode;
        }

        public PublishedFileDetails? GetPublishedFileDetails(uint appId, ulong pubFile)
        {
            var pubFileRequest = new CPublishedFile_GetDetails_Request { appid = appId };
            pubFileRequest.publishedfileids.Add(pubFile);

            var completed = false;
            PublishedFileDetails? details = null;

            Action<SteamUnifiedMessages.ServiceMethodResponse> cbMethod = callback =>
            {
                completed = true;
                if (callback.Result == EResult.OK)
                {
                    var response = callback.GetDeserializedResponse<CPublishedFile_GetDetails_Response>();
                    details = response.publishedfiledetails.FirstOrDefault();
                }
                else
                {
                    throw new Exception($"EResult {(int)callback.Result} ({callback.Result}) while retrieving file details for pubfile {pubFile}.");
                }
            };

            WaitUntilCallback(() =>
            {
                return _callbackManager.Subscribe(steamPublishedFile.SendMessage(api => api.GetDetails(pubFileRequest)), cbMethod);
            }, () => { return completed; });

            return details;
        }

        public List<PublishedFileDetails> GetPublishedFileDetails(uint appId, IEnumerable<ulong> pubFiles)
        {
            var pubFileRequest = new CPublishedFile_GetDetails_Request { appid = appId };
            pubFileRequest.publishedfileids.AddRange(pubFiles);

            var completed = false;
            List<PublishedFileDetails> detailsList = new List<PublishedFileDetails>();

            Action<SteamUnifiedMessages.ServiceMethodResponse> cbMethod = callback =>
            {
                completed = true;
                if (callback.Result == EResult.OK)
                {
                    var response = callback.GetDeserializedResponse<CPublishedFile_GetDetails_Response>();
                    detailsList.AddRange(response.publishedfiledetails);
                }
                else
                {
                    throw new Exception($"EResult {(int)callback.Result} ({callback.Result}) while retrieving file details for pubfiles ({string.Join(",", pubFiles)}).");
                }
            };

            WaitUntilCallback(() =>
            {
                return _callbackManager.Subscribe(steamPublishedFile.SendMessage(api => api.GetDetails(pubFileRequest)), cbMethod);
            }, () => { return completed; });

            return detailsList;
        }

        public void RequestAppInfo(uint appId, bool bForce = false)
        {
            if ((AppInfo.ContainsKey(appId) && !bForce) || !_isRunning)
                return;

            var completed = false;
            Action<SteamApps.PICSTokensCallback> cbMethodTokens = appTokens =>
            {
                completed = true;
                if (appTokens.AppTokensDenied.Contains(appId))
                {
                    Log.Write("Insufficient privileges to get access token for app {0}", LogSeverity.Error, appId);
                }

                foreach (var token_dict in appTokens.AppTokens)
                {
                    this.AppTokens[token_dict.Key] = token_dict.Value;
                }
            };

            WaitUntilCallback(() =>
            {
                return _callbackManager.Subscribe(Apps.PICSGetAccessTokens(new List<uint> { appId }, new List<uint>()), cbMethodTokens);
            }, () => { return completed; });

            completed = false;
            Action<SteamApps.PICSProductInfoCallback> cbMethod = appInfo =>
            {
                completed = !appInfo.ResponsePending;

                foreach (var app_value in appInfo.Apps)
                {
                    var app = app_value.Value;

                    Log.Write("Got AppInfo for {0}", LogSeverity.Debug, app.ID);
                    AppInfo[app.ID] = app;
                }

                foreach (var app in appInfo.UnknownApps)
                {
                    AppInfo[app] = null;
                }
            };

            var request = new SteamApps.PICSRequest(appId);
            if (AppTokens.ContainsKey(appId))
            {
                request.AccessToken = AppTokens[appId];
            }

            WaitUntilCallback(() =>
            {
                return _callbackManager.Subscribe(Apps.PICSGetProductInfo(new List<SteamApps.PICSRequest> { request }, new List<SteamApps.PICSRequest>()), cbMethod);
            }, () => { return completed; });
        }

        public void RequestDepotKey(uint depotId, uint appid = 0)
        {
            if (DepotKeys.ContainsKey(depotId) || !_isRunning)
                return;

            var completed = false;

            Action<SteamApps.DepotKeyCallback> cbMethod = depotKey =>
            {
                completed = true;
                Log.Write("Got depot key for {0} result: {1}", LogSeverity.Debug, depotKey.DepotID, depotKey.Result);

                if (depotKey.Result != EResult.OK)
                {
                    Disconnect();
                    return;
                }

                DepotKeys[depotKey.DepotID] = depotKey.DepotKey;
            };

            WaitUntilCallback(() =>
            {
                return _callbackManager.Subscribe(Apps.GetDepotDecryptionKey(depotId, appid), cbMethod);
            }, () => { return completed; });
        }

        public bool RequestFreeAppLicense(uint appId)
        {
            var success = false;
            var completed = false;
            Action<SteamApps.FreeLicenseCallback> cbMethod = resultInfo =>
            {
                completed = true;
                success = resultInfo.GrantedApps.Contains(appId);
            };

            WaitUntilCallback(() =>
            {
                return _callbackManager.Subscribe(Apps.RequestFreeLicense(appId), cbMethod);
            }, () => { return completed; });

            return success;
        }

        public void RequestPackageInfo(IEnumerable<uint> packageIds)
        {
            var packages = packageIds.ToList();
            packages.RemoveAll(pid => PackageInfo.ContainsKey(pid));

            if (packages.Count == 0 || !_isRunning)
                return;

            var completed = false;
            Action<SteamApps.PICSProductInfoCallback> cbMethod = packageInfo =>
            {
                completed = !packageInfo.ResponsePending;

                foreach (var package_value in packageInfo.Packages)
                {
                    var package = package_value.Value;
                    PackageInfo[package.ID] = package;
                }

                foreach (var package in packageInfo.UnknownPackages)
                {
                    PackageInfo[package] = null;
                }
            };

            var packageRequests = new List<SteamApps.PICSRequest>();

            foreach (var package in packages)
            {
                var request = new SteamApps.PICSRequest(package);

                if (PackageTokens.TryGetValue(package, out var token))
                {
                    request.AccessToken = token;
                }

                packageRequests.Add(request);
            }

            WaitUntilCallback(() =>
            {
                return _callbackManager.Subscribe(Apps.PICSGetProductInfo(new List<SteamApps.PICSRequest>(), packageRequests), cbMethod);
            }, () => { return completed; });
        }

        public bool WaitUntilCallback(Func<IDisposable> submitter, WaitCondition waiter)
        {
            IDisposable? subscription = null;
            while (_isRunning && !waiter())
            {
                lock (steamLock)
                {
                    subscription = submitter();
                }

                var session = currentSessionIndex;
                do
                {
                    lock (steamLock)
                    {
                        WaitForCallbacks();
                    }
                } while (_isRunning && currentSessionIndex == session && !waiter());
            }

            subscription?.Dispose();
            return _isRunning;
        }

        private void LicenseListCallback(SteamApps.LicenseListCallback licenseList)
        {
            if (licenseList.Result != EResult.OK)
            {
                Log.Write("Unable to get license list: {0} ", LogSeverity.Error, licenseList.Result);
                Disconnect();

                return;
            }

            Log.Write("Got {0} licenses for account!", LogSeverity.Info, licenseList.LicenseList.Count);
            Licenses = licenseList.LicenseList;

            foreach (var license in licenseList.LicenseList)
            {
                if (license.AccessToken > 0)
                {
                    PackageTokens.TryAdd(license.PackageID, license.AccessToken);
                }
            }
        }

        private void WaitForCallbacks()
        {
            _callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));

            var diff = DateTime.Now - connectTime;

            if (diff > STEAM3_TIMEOUT && !Client.IsConnected)
            {
                Log.Write("Timeout connecting to Steam3.", LogSeverity.Error);
                Disconnect();
                TimedOut?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}