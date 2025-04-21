using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using tot_lib;
using Trebuchet.Assets;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.Utils;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels.Panels
{
    public class DashboardPanel : ReactiveObject, ITickingPanel, IRefreshablePanel, IDisplablePanel, IRefreshingPanel, IBottomPanel
    {
        public DashboardPanel(
            AppSetup setup, 
            UIConfig uiConfig, 
            AppFiles appFiles, 
            SteamApi steamApi,
            Launcher launcher, 
            DialogueBox box,
            TaskBlocker blocker,
            ILogger<DashboardPanel> logger) 
        {
            _setup = setup;
            _uiConfig = uiConfig;
            _appFiles = appFiles;
            _steamApi = steamApi;
            _launcher = launcher;
            _box = box;
            _blocker = blocker;
            _logger = logger;
            CanBeOpened = Tools.IsClientInstallValid(_setup.Config) || Tools.IsServerInstallValid(_setup.Config);

            var canDownloadServer = blocker.WhenAnyValue(x => x.CanDownloadServer);
            var canDownloadMods = blocker.WhenAnyValue(x => x.CanDownloadMods);
            CloseAllCommand = ReactiveCommand.CreateFromTask(OnCloseAll);
            KillAllCommand = ReactiveCommand.CreateFromTask(OnKillAll);
            LaunchAllCommand = ReactiveCommand.CreateFromTask(
                canExecute: canDownloadServer, execute: OnLaunchAll);
            UpdateServerCommand = ReactiveCommand.CreateFromTask(
                canExecute: canDownloadServer, execute: UpdateServer);
            UpdateAllModsCommand = ReactiveCommand.CreateFromTask(
                canExecute: canDownloadMods, execute:UpdateMods);
            VerifyFilesCommand = ReactiveCommand.CreateFromTask(
                canExecute: canDownloadMods, execute:OnFileVerification);

            Client = new ClientInstanceDashboard(new ProcessStatsLight(), _blocker, _box);
            ConfigureClient(Client);
            RefreshClientSelection(_setup.Config.SelectedClientProfile, _setup.Config.SelectedClientModlist);
        }
        
        private readonly AppSetup _setup;
        private readonly UIConfig _uiConfig;
        private readonly AppFiles _appFiles;
        private readonly SteamApi _steamApi;
        private readonly Launcher _launcher;
        private readonly DialogueBox _box;
        private readonly TaskBlocker _blocker;
        private readonly ILogger<DashboardPanel> _logger;
        private DateTime _lastUpdateCheck = DateTime.MinValue;
        private bool _canBeOpened;

        public string Icon => @"mdi-view-dashboard";
        public string Label => Resources.PanelDashboard;

        public bool CanBeOpened
        {
            get => _canBeOpened;
            set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
        }

        public bool CanDisplayServers => _setup.Config is { ServerInstanceCount: > 0 };
        public ClientInstanceDashboard Client { get; }
        public ObservableCollection<ServerInstanceDashboard> Instances { get; } = [];
        public ReactiveCommand<Unit,Unit> CloseAllCommand { get; }
        public ReactiveCommand<Unit,Unit> KillAllCommand { get; }
        public ReactiveCommand<Unit,Unit> LaunchAllCommand { get; }
        public ReactiveCommand<Unit,Unit> UpdateAllModsCommand { get; }
        public ReactiveCommand<Unit,Unit> UpdateServerCommand { get; }
        public ReactiveCommand<Unit,Unit> VerifyFilesCommand { get; }
        
        public event AsyncEventHandler? RequestRefresh;


        /// <summary>
        ///     Collect all used mods of all the client and server instances and update them. Will not perform any action if the
        ///     game is running or the main task is blocked.
        /// </summary>
        public async Task UpdateMods()
        {
            _logger.LogInformation(@"Updating mods");
            try
            {
                var modlists = Instances.Select(i => i.SelectedModlist).ToList();
                modlists.Add(Client.SelectedModlist);
                var mods = modlists.Distinct()
                    .Select(l => _appFiles.Mods.CollectAllMods(l))
                    .SelectMany(x => x)
                    .Distinct().ToList();
                await _steamApi.UpdateMods(mods);
                await OnRequestRefresh();
            }
            catch (Exception tex)
            {
                _logger.LogError(tex, @"Failed");
                await _box.OpenErrorAsync(tex.Message);
            }
        }

        /// <summary>
        ///     Update all server instances. Will not perform any action if the game is running or the main task is blocked.
        /// </summary>
        public async Task UpdateServer()
        {
            _logger.LogInformation(@"Updating servers");
            try
            {
                await _steamApi.UpdateServers();
                await RefreshPanel();
            }
            catch (Exception tex)
            {
                _logger.LogError(tex, @"Failed");
                await _box.OpenErrorAsync(tex.Message);
            }
        }
        
        public async Task KillClient()
        {
            if (!Client.ProcessRunning) return;

            if (_uiConfig.DisplayWarningOnKill)
            {
                var confirm = new OnBoardingConfirmation(Resources.Kill, Resources.KillText);
                await _box.OpenAsync(confirm);
                if (!confirm.Result) return;
            }

            _logger.LogWarning(@"Killing client");
            try
            {
                await _launcher.KillClient();
            }
            catch (Exception tex)
            {
                _logger.LogError(tex, @"Failed");
                await _box.OpenErrorAsync(tex.Message);
            }
        }
        
        public async Task LaunchClient(bool autoConnect)
        {
            if (Client.ProcessRunning) return;
            if (!await CheckForSteamClientRunning()) return;

            var data = new Dictionary<string, object>
            {
                { @"autoConnect", autoConnect },
                { @"isBattleEye", Client.BattleEye }
            };
            using(_logger.BeginScope(data))
                _logger.LogInformation(@"Launching client");
            Client.CanLaunch = false;
            try
            {
                if (_setup.Config.AutoUpdateStatus != AutoUpdateStatus.Never && !_launcher.IsAnyServerRunning())
                {
                    var modlist = _appFiles.Mods.CollectAllMods(Client.SelectedModlist).ToList();
                    await _steamApi.UpdateMods(modlist);
                }

                _setup.Config.SelectedClientProfile = Client.SelectedProfile;
                _setup.Config.SelectedClientModlist = Client.SelectedModlist;
                await _launcher.CatapultClient(Client.BattleEye, autoConnect);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, @"Failed");
                await _box.OpenErrorAsync(ex.Message);
                Client.CanLaunch = true;
            }
        }
        
        public async Task CloseServer(int instance)
        {
            var dashboard = GetServerInstance(instance);
            if (!dashboard.ProcessRunning) return;

            _logger.LogInformation(@"Closing server {instance}", instance);
            await _launcher.CloseServer(instance);
        }
        
        public async Task KillServer(int instance)
        {
            var dashboard = GetServerInstance(instance);
            if (!dashboard.ProcessRunning) return;

            if (_uiConfig.DisplayWarningOnKill)
            {
                var confirm = new OnBoardingConfirmation(Resources.Kill, Resources.KillText);
                await _box.OpenAsync(confirm);
                if (!confirm.Result) return;
            }

            _logger.LogWarning(@"Killing server {instance}", instance);
            await _launcher.KillServer(dashboard.Instance);
        }
        
        public async Task LaunchServer(int instance)
        {
            var dashboard = GetServerInstance(instance);
            if(dashboard.ProcessRunning) return;
            dashboard.CanLaunch = false;

            using(_logger.BeginScope((@"instance", instance)))
                _logger.LogInformation(@"Launching server");
            
            try
            {
                if (_setup.Config.AutoUpdateStatus != AutoUpdateStatus.Never && !_launcher.IsAnyServerRunning() &&
                    !_launcher.IsClientRunning())
                {
                    _logger.LogInformation(@"Update before launch");
                    var modlist = _appFiles.Mods.CollectAllMods(dashboard.SelectedModlist).ToList();
                    await _steamApi.UpdateServers();
                    await _steamApi.UpdateMods(modlist);
                }

                _setup.Config.SetInstanceParameters(dashboard.Instance, dashboard.SelectedModlist,
                    dashboard.SelectedProfile);
                await _launcher.CatapultServer(dashboard.Instance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, @"Failed");
                await _box.OpenErrorAsync(ex.Message);
                dashboard.CanLaunch = true;
            }
        }

        public ServerInstanceDashboard GetServerInstance(int instance)
        {
            if(instance < 0 || instance >= Instances.Count)
                throw new Exception(@"Instance out of range");
            return Instances[instance];
        }

        public async Task TickPanel()
        {
            await Client.ProcessRefresh(_launcher.GetClientProcess(), _uiConfig.DisplayProcessPerformance);
            foreach (var instance in _launcher.GetServerProcesses())
                await Instances[instance.Instance].ProcessRefresh(instance, _uiConfig.DisplayProcessPerformance);

            if ((DateTime.UtcNow - _lastUpdateCheck).TotalSeconds >= 300 && _uiConfig.AutoRefreshModlist)
            {
                _lastUpdateCheck = DateTime.UtcNow;
                await CheckModUpdatesAsync();
            }
        }

        public Task RefreshPanel()
        {
            _logger.LogDebug(@"Refresh panel");
            CanBeOpened = Tools.IsClientInstallValid(_setup.Config) || Tools.IsServerInstallValid(_setup.Config);
            Client.CanUseDashboard = Tools.IsClientInstallValid(_setup.Config);
            int installedCount = _steamApi.GetInstalledServerInstanceCount();
            foreach (var instance in Instances)
            {
                instance.CanUseDashboard = _setup.Config.ServerInstanceCount > instance.Instance &&
                                           installedCount > instance.Instance;
            }
            
            return Task.CompletedTask;
        }

        public async Task DisplayPanel()
        {
            _logger.LogDebug(@"Display panel");
            CreateInstancesIfNeeded();
            RefreshClientSelection();
            RefreshServerSelection();
            await RefreshPanel();
            await CheckModUpdatesAsync();
        }

        private void ConfigureClient(ClientInstanceDashboard client)
        {
            client.ModlistSelected += (_, modlist) =>
            {
                _setup.Config.SelectedClientModlist = modlist;
                _setup.Config.SaveFile();
                return CheckModUpdatesAsync();
            };
            client.ProfileSelected += (_, profile) =>
            {
                _setup.Config.SelectedClientProfile = profile;
                _setup.Config.SaveFile();
                return Task.CompletedTask;
            };
            client.KillClicked += (_, _)  => KillClient();
            client.LaunchClicked += (_, autoConnect) => LaunchClient(autoConnect);
            client.UpdateClicked += (_, _) => UpdateMods();
        }

        private void RefreshClientSelection()
        {
            RefreshClientSelection(Client.SelectedProfile, Client.SelectedModlist);
        }
        
        private void RefreshClientSelection(string profile, string modlist)
        {
            modlist = _appFiles.Mods.ResolveProfile(modlist);
            profile = _appFiles.Client.ResolveProfile(profile);

            Client.Modlists = _appFiles.Mods.ListProfiles().ToList();
            Client.Profiles = _appFiles.Client.ListProfiles().ToList();
            Client.SelectedProfile = profile;
            Client.SelectedModlist = modlist;
        }

        private void RefreshServerSelection()
        {
            foreach (var dashboard in Instances)
                RefreshServerSelection(dashboard);
        }

        private void RefreshServerSelection(ServerInstanceDashboard dashboard)
        {
            var modlist = _appFiles.Mods.ResolveProfile(dashboard.SelectedModlist);
            var profile = _appFiles.Server.ResolveProfile(dashboard.SelectedProfile);
            
            dashboard.Profiles = _appFiles.Server.ListProfiles().ToList();
            dashboard.Modlists = _appFiles.Mods.ListProfiles().ToList();
            dashboard.SelectedModlist = modlist;
            dashboard.SelectedProfile = profile;
        }

        private void RefreshClientNeededUpdates(List<ulong> neededUpdates)
        {
            var mods = _appFiles.Mods.CollectAllMods(Client.SelectedModlist).ToList();
            Client.UpdateNeeded = neededUpdates.Intersect(mods).ToList();
        }

        private void RefreshServerNeededUpdates(List<ulong> neededUpdates)
        {
            foreach (var dashboard in Instances)
                RefreshServerNeededUpdates(dashboard, neededUpdates);
        }

        private void RefreshServerNeededUpdates(ServerInstanceDashboard dashboard, List<ulong> neededUpdates)
        {
            var mods = _appFiles.Mods.CollectAllMods(dashboard.SelectedModlist).ToList();
            dashboard.UpdateNeeded = neededUpdates.Intersect(mods).ToList();
        }

        private Task CheckModUpdatesAsync()
        {
            var modlists = Instances.Select(i => i.SelectedModlist).ToList();
            modlists.Add(Client.SelectedModlist);
            return CheckModUpdatesAsync(modlists);
        }
        
        private async Task CheckModUpdatesAsync(List<string> modlists)
        {
            _logger.LogInformation(@"Check for mod updates");
            try
            {
                var mods = modlists.Distinct()
                    .Select(l => _appFiles.Mods.CollectAllMods(l))
                    .SelectMany(x => x)
                    .Distinct().ToList();
                var response = await _steamApi.RequestModDetails(mods);
                var neededUpdates = _steamApi.CheckModsForUpdate(response.GetManifestKeyValuePairs().ToList());
                RefreshClientNeededUpdates(neededUpdates);
                RefreshServerNeededUpdates(neededUpdates);
            }
            catch (Exception tex)
            {
                _logger.LogError(tex, @"Failed");
                await _box.OpenErrorAsync(tex.Message);
            }
        }

        private void CreateInstancesIfNeeded()
        {
            if (Instances.Count >= _setup.Config.ServerInstanceCount)
                return;

            for (var i = Instances.Count; i < _setup.Config.ServerInstanceCount; i++)
            {
                var instance = new ServerInstanceDashboard(i, new ProcessStatsLight(), _blocker, _box)
                    {
                        SelectedModlist = _setup.Config.GetInstanceModlist(i),
                        SelectedProfile = _setup.Config.GetInstanceProfile(i)
                    };
                RegisterServerInstanceEvents(instance);
                Instances.Add(instance);
            }
        }

        private void RegisterServerInstanceEvents(ServerInstanceDashboard instance)
        {
            instance.ModlistSelected += (_, arg) =>
            {
                _setup.Config.SetInstanceModlist(arg.Instance, arg.Selection);
                _setup.Config.SaveFile();
                return CheckModUpdatesAsync();
            };
            instance.ProfileSelected += (_, arg) =>
            {
                _setup.Config.SetInstanceProfile(arg.Instance, arg.Selection);
                _setup.Config.SaveFile();
                return Task.CompletedTask;
            };
            instance.KillClicked += (_, arg) => KillServer(arg);
            instance.LaunchClicked += (_, arg) => LaunchServer(arg);
            instance.CloseClicked += (_, arg) => CloseServer(arg);
            instance.UpdateClicked += (_, _) => UpdateMods();
        }

        private async Task OnCloseAll()
        {
            foreach (var i in Instances)
                await CloseServer(i.Instance);
        }

        private async Task OnFileVerification()
        {
            var confirm = new OnBoardingConfirmation(Resources.VerifyFiles, Resources.VerifyFilesText);
            await _box.OpenAsync(confirm);
            if (!confirm.Result) return;

            _logger.LogInformation(@"File Verification");
            try
            {
                var modlists = Instances.Select(i => i.SelectedModlist).ToList();
                modlists.Add(Client.SelectedModlist);
                var mods = modlists.Distinct()
                    .Select(l => _appFiles.Mods.CollectAllMods(l))
                    .SelectMany(x => x)
                    .Distinct().ToList();
                
                await _steamApi.VerifyFiles(mods);
            }
            catch (Exception tex)
            {
                _logger.LogError(tex, @"Failed");
                await _box.OpenErrorAsync(tex.Message);
            }
        }

        private async Task OnKillAll()
        {
            foreach (var i in Instances)
                await KillServer(i.Instance);
        }

        private async Task OnLaunchAll()
        {
            foreach(var i in Instances)
                await LaunchServer(i.Instance);
        }

        private async Task OnRequestRefresh()
        {
            if (RequestRefresh is not null)
                await RequestRefresh.Invoke(this, EventArgs.Empty);
        }

        private async Task<bool> CheckForSteamClientRunning()
        {
            var process = await Tools.GetProcessesWithName(Constants.SteamClientExe);
            if (process.Count > 0) return true;

            await _box.OpenErrorAsync(Resources.OnBoardingSteamClientOffline,
                Resources.OnBoardingSteamClientOfflineSub);
            return false;
        }
    }
}