using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            Launcher launcher, 
            DialogueBox box,
            TaskBlocker blocker,
            Steam steam,
            Operations operations,
            ILogger<DashboardPanel> logger) 
        {
            _setup = setup;
            _uiConfig = uiConfig;
            _appFiles = appFiles;
            _launcher = launcher;
            _box = box;
            _blocker = blocker;
            _steam = steam;
            _operations = operations;
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
        private readonly Launcher _launcher;
        private readonly DialogueBox _box;
        private readonly TaskBlocker _blocker;
        private readonly Steam _steam;
        private readonly Operations _operations;
        private readonly ILogger<DashboardPanel> _logger;
        private DateTime _lastUpdateCheck = DateTime.MinValue;
        private bool _canBeOpened;
        private bool _serverUpdateAvailable;
        private bool _anyModUpdate;

        public string Icon => @"mdi-view-dashboard";
        public string Label => Resources.PanelDashboard;

        public bool CanBeOpened
        {
            get => _canBeOpened;
            set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
        }

        public bool ServerUpdateAvailable
        {
            get => _serverUpdateAvailable;
            set => this.RaiseAndSetIfChanged(ref _serverUpdateAvailable, value);
        }

        public bool AnyModUpdate
        {
            get => _anyModUpdate;
            set => this.RaiseAndSetIfChanged(ref _anyModUpdate, value);
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
                var modlists = Instances.Select(i => i.SelectedModlist?.ModList).OfType<IPRefWithModList>().ToList();
                if(Client.SelectedModlist?.ModList is not null)
                    modlists.Add(Client.SelectedModlist.ModList);
                var mods = modlists.Distinct()
                    .SelectMany(l => l.GetModsFromList())
                    .Distinct().ToList();
                await _operations.UpdateMods(mods);
                _lastUpdateCheck = DateTime.MinValue;
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
                await _operations.UpdateServers();
                await RefreshPanel();
                _lastUpdateCheck = DateTime.MinValue;
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
        
        public async Task LaunchClient(ClientConnectionRef? autoConnect)
        {
            if (Client.SelectedModlist?.ModList is null || Client.SelectedProfile is null) return;
            if (Client.ProcessRunning) return;
            if (!await CheckForSteamClientRunning()) return;

            var data = new Dictionary<string, object>
            {
                { @"autoConnect", autoConnect?.Connection ?? string.Empty },
                { @"isBattleEye", Client.BattleEye }
            };
            using(_logger.BeginScope(data))
                _logger.LogInformation(@"Launching client");
            Client.CanLaunch = false;
            try
            {
                if (_setup.Config.AutoUpdateStatus != AutoUpdateStatus.Never && !_launcher.IsAnyServerRunning())
                {
                    var modlist = Client.SelectedModlist.ModList.GetModsFromList().ToList();
                    await _operations.UpdateMods(modlist);
                }

                _setup.Config.SelectedClientProfile = Client.SelectedProfile.Uri.OriginalString;
                _setup.Config.SelectedClientModlist = Client.SelectedModlist.ModList.Uri.OriginalString;
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
            if (dashboard.SelectedModlist is null || dashboard.SelectedProfile is null) return;
            
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
                    var modlist = dashboard.SelectedModlist.ModList.GetModsFromList().ToList();
                    await _operations.UpdateServers();
                    await _operations.UpdateMods(modlist);
                    _lastUpdateCheck = DateTime.MinValue;
                }

                _setup.Config.SetInstanceParameters(dashboard.Instance, 
                    dashboard.SelectedModlist.ModList.Uri.OriginalString,
                    dashboard.SelectedProfile.Uri.OriginalString);
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
            await _launcher.Tick();
            
            await Client.ProcessRefresh(_launcher.GetClientProcess(), _uiConfig.DisplayProcessPerformance);
            foreach (var instance in _launcher.GetServerProcesses())
                await Instances[instance.Instance].ProcessRefresh(instance, _uiConfig.DisplayProcessPerformance);

            if ((DateTime.UtcNow - _lastUpdateCheck).TotalSeconds >= 300 && _uiConfig.AutoRefreshModlist)
            {
                _lastUpdateCheck = DateTime.UtcNow;
                await CheckModUpdatesAsync();
                ServerUpdateAvailable = await _operations.CheckServerUpdate();
            }
        }

        public Task RefreshPanel()
        {
            _logger.LogDebug(@"Refresh panel");
            CanBeOpened = Tools.IsClientInstallValid(_setup.Config) || Tools.IsServerInstallValid(_setup.Config);
            Client.CanUseDashboard = Tools.IsClientInstallValid(_setup.Config);
            int installedCount = _operations.GetInstalledServerInstanceCount();
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
        }

        private void ConfigureClient(ClientInstanceDashboard client)
        {
            client.ModlistSelected += (_, modlist) =>
            {
                _setup.Config.SelectedClientModlist = modlist.Uri.OriginalString;
                _setup.Config.SaveFile();
                return CheckModUpdatesAsync();
            };
            client.ProfileSelected += (_, profile) =>
            {
                _setup.Config.SelectedClientProfile = profile.Uri.OriginalString;
                _setup.Config.SaveFile();
                return Task.CompletedTask;
            };
            client.KillClicked += (_, _)  => KillClient();
            client.LaunchClicked += (_, autoConnect) => LaunchClient(autoConnect);
            client.UpdateClicked += (_, _) => UpdateMods();
        }

        private void RefreshClientSelection()
        {
            RefreshClientSelection(Client.SelectedProfile, Client.SelectedModlist?.ModList);
            Client.RefreshPanel();
        }

        private void RefreshClientSelection(string profile, string modlist)
        {
            RefreshClientSelection(_appFiles.Client.Resolve(profile), _appFiles.ResolveModList(modlist));
        }
        private void RefreshClientSelection(ClientProfileRef? profile, IPRefWithModList? modlist)
        {
            profile = profile is null ? _appFiles.Client.GetDefault() : _appFiles.Client.Resolve(profile);
            modlist = modlist is null ? _appFiles.Mods.GetDefault() : modlist.Resolve();
            
            Client.Modlists = [];
            Client.Modlists.AddRange(_appFiles.Mods.GetList().Select(x => new ModListRefViewModel(x)));
            Client.Modlists.AddRange(_appFiles.Sync.GetList().Select(x => new ModListRefViewModel(x)));
            
            Client.Profiles = _appFiles.Client.GetList().ToList();
            Client.SelectedProfile = profile;
            Client.SelectedModlist = new ModListRefViewModel(modlist);
        }

        private void RefreshServerSelection()
        {
            foreach (var dashboard in Instances)
                RefreshServerSelection(dashboard);
        }

        private void RefreshServerSelection(ServerInstanceDashboard dashboard)
        {
            dashboard.SelectedModlist = new ModListRefViewModel(dashboard.SelectedModlist?.ModList is null 
                ? _appFiles.Mods.GetDefault() 
                : dashboard.SelectedModlist.ModList.Resolve());
            dashboard.SelectedProfile = dashboard.SelectedProfile is null
                ? _appFiles.Server.GetDefault()
                : _appFiles.Server.Resolve(dashboard.SelectedProfile);
            
            dashboard.Profiles = _appFiles.Server.GetList().ToList();
            dashboard.Modlists = [];
            dashboard.Modlists.AddRange(_appFiles.Mods.GetList().Select(x => new ModListRefViewModel(x)));
            dashboard.Modlists.AddRange(_appFiles.Sync.GetList().Select(x => new ModListRefViewModel(x)));
        }

        private void RefreshClientNeededUpdates(List<PublishedMod> neededUpdates)
        {
            if (Client.SelectedModlist is null)
            {
                Client.UpdateNeeded = [];
                return;
            }
            var mods = Client.SelectedModlist.ModList.GetModsFromList().ToList();
            Client.UpdateNeeded = neededUpdates
                .Select(x => x.PublishedFileId)
                .Intersect(mods)
                .ToList();
        }

        private void RefreshServerNeededUpdates(List<PublishedMod> neededUpdates)
        {
            foreach (var dashboard in Instances)
                RefreshServerNeededUpdates(dashboard, neededUpdates);
        }

        private void RefreshServerNeededUpdates(ServerInstanceDashboard dashboard, List<PublishedMod> neededUpdates)
        {
            if (dashboard.SelectedModlist is null)
            {
                dashboard.UpdateNeeded = [];
                return;
            }
            var mods = dashboard.SelectedModlist.ModList.GetModsFromList().ToList();
            dashboard.UpdateNeeded = neededUpdates
                .Select(x => x.PublishedFileId)
                .Intersect(mods)
                .ToList();
        }

        private Task CheckModUpdatesAsync()
        {
            var modlists = Instances.Select(i => i.SelectedModlist?.ModList).OfType<IPRefWithModList>().ToList();
            if(Client.SelectedModlist?.ModList is not null)
                modlists.Add(Client.SelectedModlist.ModList);
            return CheckModUpdatesAsync(modlists);
        }
        
        private async Task CheckModUpdatesAsync(List<IPRefWithModList> modlists)
        {
            _logger.LogInformation(@"Check for mod updates");
            try
            {
                var mods = modlists.Distinct()
                    .SelectMany(x => x.GetModsFromList())
                    .Distinct().ToList();
                var response = (await _steam.RequestModDetails(mods))
                    .Where(x => x.Status.Status != UGCStatus.UpToDate)
                    .ToList();
                RefreshClientNeededUpdates(response);
                RefreshServerNeededUpdates(response);
                AnyModUpdate = response.Count > 0;
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
                var instance = new ServerInstanceDashboard(i, new ProcessStatsLight(), _blocker, _box);
                instance.SelectedModlist = new ModListRefViewModel(_appFiles.ResolveModList(_setup.Config.GetInstanceModlist(i)));
                instance.SelectedProfile = _appFiles.Server.Resolve(_setup.Config.GetInstanceProfile(i));
                
                RegisterServerInstanceEvents(instance);
                Instances.Add(instance);
            }
        }

        private void RegisterServerInstanceEvents(ServerInstanceDashboard instance)
        {
            instance.ModlistSelected += (_, arg) =>
            {
                _setup.Config.SetInstanceModlist(arg.Instance, arg.Selection.Uri.OriginalString);
                _setup.Config.SaveFile();
                return CheckModUpdatesAsync();
            };
            instance.ProfileSelected += (_, arg) =>
            {
                _setup.Config.SetInstanceProfile(arg.Instance, arg.Selection.Uri.OriginalString);
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
                var modlists = Instances.Select(i => i.SelectedModlist?.ModList).OfType<IPRefWithModList>().ToList();
                if(Client.SelectedModlist?.ModList is not null)
                    modlists.Add(Client.SelectedModlist.ModList);
                var mods = modlists.Distinct()
                    .SelectMany(l => l.GetModsFromList())
                    .Distinct().ToList();
                
                await _operations.VerifyFiles(mods);
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