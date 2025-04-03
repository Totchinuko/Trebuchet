﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using SteamKit2.GC.Dota.Internal;
using Trebuchet.Assets;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet.ViewModels.Panels
{
    public class DashboardPanel : Panel
    {
        private readonly AppSetup _setup;
        private readonly UIConfig _uiConfig;
        private readonly AppFiles _appFiles;
        private readonly SteamAPI _steamApi;
        private readonly Launcher _launcher;
        private readonly ILogger<DashboardPanel> _logger;
        private DispatcherTimer _timer;

        public DashboardPanel(
            AppSetup setup, 
            UIConfig uiConfig, 
            AppFiles appFiles, 
            SteamAPI steamApi,
            Launcher launcher, 
            ILogger<DashboardPanel> logger) : 
            base(Resources.Dashboard, "mdi-view-dashboard", true)
        {
            _setup = setup;
            _uiConfig = uiConfig;
            _appFiles = appFiles;
            _steamApi = steamApi;
            _launcher = launcher;
            _logger = logger;
            CloseAllCommand.Subscribe(OnCloseAll);
            KillAllCommand.Subscribe(OnKillAll);
            LaunchAllCommand
                .SetBlockingType<SteamDownload>()
                .Subscribe(OnLaunchAll);
            UpdateServerCommand
                    .SetBlockingType<SteamDownload>()
                    .Subscribe(UpdateServer);
            UpdateAllModsCommand
                .SetBlockingType<SteamDownload>()
                .SetBlockingType<ClientRunning>()
                .SetBlockingType<ServersRunning>()
                .Subscribe(UpdateMods);
            VerifyFilesCommand
                .SetBlockingType<SteamDownload>()
                .SetBlockingType<ClientRunning>()
                .SetBlockingType<ServersRunning>()
                .Subscribe(OnFileVerification);

            Client = new ClientInstanceDashboard(new ProcessStatsLight());
            Initialize();

            _timer = new DispatcherTimer(TimeSpan.FromMinutes(5), DispatcherPriority.Background, (_,_) => OnCheckModUpdate());
        }

        public bool CanDisplayServers => _setup.Config is { ServerInstanceCount: > 0 };
        public ClientInstanceDashboard Client { get; }
        public ObservableCollection<ServerInstanceDashboard> Instances { get; } = new();
        public SimpleCommand CloseAllCommand { get; private set; } = new();
        public SimpleCommand KillAllCommand { get; private set; } = new();
        public TaskBlockedCommand LaunchAllCommand { get; private set; } = new();
        public TaskBlockedCommand UpdateAllModsCommand { get; private set; } = new();
        public TaskBlockedCommand UpdateServerCommand { get; private set; } = new();
        public TaskBlockedCommand VerifyFilesCommand { get; private set; } = new();

        public override bool CanExecute(object? parameter)
        {
            return (Tools.IsClientInstallValid(_setup.Config) || Tools.IsServerInstallValid(_setup.Config));
        }

        /// <summary>
        ///     Collect all used modlists of all the client and server instances. Can have duplicates.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> CollectAllModlistNames()
        {
            if (Client.CanUseDashboard && !string.IsNullOrEmpty(Client.SelectedModlist))
                yield return Client.SelectedModlist;

            foreach (var i in Instances)
                if (i.CanUseDashboard && !string.IsNullOrEmpty(i.SelectedModlist))
                    yield return i.SelectedModlist;
        }

        public override async void PanelDisplayed()
        {
            CreateInstancesIfNeeded();
            RefreshClientSelection();
            RefreshServerSelection();
            RefreshDashboards();
            await CheckModUpdates();
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            RefreshDashboards();
        }

        public void RefreshDashboards()
        {
            Client.CanUseDashboard = Tools.IsClientInstallValid(_setup.Config);
            int installedCount = _steamApi.GetInstalledServerInstanceCount();
            foreach (var instance in Instances)
            {
                instance.CanUseDashboard = _setup.Config.ServerInstanceCount > instance.Instance &&
                                           installedCount > instance.Instance;
            }
        }

        /// <summary>
        ///     Collect all used mods of all the client and server instances and update them. Will not perform any action if the
        ///     game is running or the main task is blocked.
        /// </summary>
        public async void UpdateMods()
        {
            try
            {
                var modlists = Instances.Select(i => i.SelectedModlist).ToList();
                modlists.Add(Client.SelectedModlist);
                var mods = modlists.Distinct()
                    .Select(l => _appFiles.Mods.CollectAllMods(l))
                    .SelectMany(x => x)
                    .Distinct().ToList();
                await _steamApi.UpdateMods(mods);
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal("Error", tex.Message).OpenDialogueAsync();
            }
        }

        /// <summary>
        ///     Update all server instances. Will not perform any action if the game is running or the main task is blocked.
        /// </summary>
        public async void UpdateServer()
        {
            try
            {
                await _steamApi.UpdateServers();
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal("Error", tex.Message).OpenDialogueAsync();
            }
        }
        
        public async void KillClient()
        {
            if (!Client.ProcessRunning) return;

            if (_uiConfig.DisplayWarningOnKill)
            {
                var question = new QuestionModal(Resources.Kill, Resources.KillText);
                await question.OpenDialogueAsync();
                if (!question.Result) return;
            }
            
            Client.KillCommand.Toggle(false);
            try
            {
                await _launcher.KillClient();
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal("Error", tex.Message).OpenDialogueAsync();
            }
        }
        
        public async void LaunchClient(bool isBattleEye)
        {
            if (Client.ProcessRunning) return;

            Client.LaunchCommand.Toggle(false);
            Client.LaunchBattleEyeCommand.Toggle(false);

            if (_setup.Config.AutoUpdateStatus != AutoUpdateStatus.Never && !_launcher.IsAnyServerRunning())
            {
                var modlist = _appFiles.Mods.CollectAllMods(Client.SelectedModlist).ToList();
                await _steamApi.UpdateMods(modlist);
            }

            _setup.Config.SelectedClientProfile = Client.SelectedProfile;
            _setup.Config.SelectedClientModlist = Client.SelectedModlist;
            await _launcher.CatapultClient(isBattleEye);
        }
        
        public async void CloseServer(int instance)
        {
            var dashboard = GetServerInstance(instance);
            if (!dashboard.ProcessRunning) return;

            dashboard.CloseCommand.Toggle(false);
            await _launcher.CloseServer(instance);
        }
        
        public async void KillServer(int instance)
        {
            var dashboard = GetServerInstance(instance);
            if (!dashboard.ProcessRunning) return;

            if (_uiConfig.DisplayWarningOnKill)
            {
                QuestionModal question = new QuestionModal(Resources.Kill, Resources.KillText);
                await question.OpenDialogueAsync();
                if (!question.Result) return;
            }

            dashboard.KillCommand.Toggle(false);
            dashboard.CloseCommand.Toggle(false);
            await _launcher.KillServer(dashboard.Instance);
        }
        
        public async void LaunchServer(int instance)
        {
            var dashboard = GetServerInstance(instance);
            if(dashboard.ProcessRunning) return;
            dashboard.LaunchCommand.Toggle(false);

            try
            {
                if (_setup.Config.AutoUpdateStatus != AutoUpdateStatus.Never && !_launcher.IsAnyServerRunning() &&
                    !_launcher.IsClientRunning())
                {
                    var modlist = _appFiles.Mods.CollectAllMods(dashboard.SelectedModlist).ToList();
                    await _steamApi.UpdateServers();
                    await _steamApi.UpdateMods(modlist);
                }

                _setup.Config.SetInstanceParameters(dashboard.Instance, dashboard.SelectedModlist,
                    dashboard.SelectedProfile);
                await _launcher.CatapultServer(dashboard.Instance);
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal("Error", tex.Message).OpenDialogueAsync();
            }
            finally
            {
                dashboard.LaunchCommand.Toggle(true);
            }
        }

        public ServerInstanceDashboard GetServerInstance(int instance)
        {
            if(instance < 0 || instance >= Instances.Count)
                throw new Exception("Instance out of range");
            return Instances[instance];
        }

        public override async Task Tick()
        {
            await Client.ProcessRefresh(_launcher.GetClientProcess(), _uiConfig.DisplayProcessPerformance);
            foreach (var instance in _launcher.GetServerProcesses())
                await Instances[instance.Instance].ProcessRefresh(instance, _uiConfig.DisplayProcessPerformance);
        }

        private async void Initialize()
        {
            Client.ModlistSelected += (_, modlist) =>
            {
                _setup.Config.SelectedClientModlist = modlist;
                _setup.Config.SaveFile();
            };
            Client.ProfileSelected += (_, profile) =>
            {
                _setup.Config.SelectedClientProfile = profile;
                _setup.Config.SaveFile();
            };
            Client.KillClicked += (_, _)  => KillClient();
            Client.LaunchClicked += (_, battleEye) => LaunchClient(battleEye);
            Client.UpdateClicked += (_, _) => UpdateMods();
            Client.SelectedModlist = _setup.Config.SelectedClientModlist;
            Client.SelectedProfile = _setup.Config.SelectedClientProfile;
            CreateInstancesIfNeeded();
            RefreshClientSelection();
            RefreshServerSelection();
            await CheckModUpdates();
        }

        private void RefreshClientSelection()
        {
            var modlist = _appFiles.Mods.ResolveProfile(Client.SelectedModlist);
            var profile = _appFiles.Client.ResolveProfile(Client.SelectedProfile);

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
            var profile = _appFiles.Client.ResolveProfile(dashboard.SelectedProfile);
            
            dashboard.Profiles = _appFiles.Client.ListProfiles().ToList();
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
        
        private async Task CheckModUpdates()
        {
            try
            {
                var modlists = Instances.Select(i => i.SelectedModlist).ToList();
                modlists.Add(Client.SelectedModlist);
                var mods = modlists.Distinct()
                    .Select(l => _appFiles.Mods.CollectAllMods(l))
                    .SelectMany(x => x)
                    .Distinct().ToList();
                var response = await _steamApi.RequestModDetails(mods);
                var neededUpdates = _steamApi.CheckModsForUpdate(response.GetManifestKeyValuePairs().ToList());
                RefreshClientNeededUpdates(neededUpdates);
                RefreshServerNeededUpdates(neededUpdates);
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal("Error", tex.Message).OpenDialogueAsync();
            }
        }

        private void CreateInstancesIfNeeded()
        {
            if (Instances.Count >= _setup.Config.ServerInstanceCount)
            {
                OnPropertyChanged(nameof(Instances));
                return;
            }

            for (var i = Instances.Count; i < _setup.Config.ServerInstanceCount; i++)
            {
                var instance = new ServerInstanceDashboard(i, new ProcessStatsLight());
                instance.SelectedModlist = _setup.Config.GetInstanceModlist(i);
                instance.SelectedProfile = _setup.Config.GetInstanceProfile(i);
                RegisterServerInstanceEvents(instance);
                Instances.Add(instance);
            }
            OnPropertyChanged(nameof(Instances));
        }

        private void RegisterServerInstanceEvents(ServerInstanceDashboard instance)
        {
            instance.ModlistSelected += (_, arg) =>
            {
                _setup.Config.SetInstanceModlist(arg.Instance, arg.Selection);
                _setup.Config.SaveFile();
            };
            instance.ProfileSelected += (_, arg) =>
            {
                _setup.Config.SetInstanceProfile(arg.Instance, arg.Selection);
                _setup.Config.SaveFile();
            };
            instance.KillClicked += (_, arg) => KillServer(arg);
            instance.LaunchClicked += (_, arg) => LaunchServer(arg);
            instance.CloseClicked += (_, arg) => CloseServer(arg);
        }

        private async void OnCheckModUpdate()
        {
            if (!_uiConfig.AutoRefreshModlist) return;
            await CheckModUpdates();
        }

        private void OnCloseAll()
        {
            foreach (var i in Instances)
                CloseServer(i.Instance);
        }

        private async void OnFileVerification()
        {
            var question = new QuestionModal("Verify files",
                "This will verify all server and mod files. This may take a while. Do you want to continue?");
            await question.OpenDialogueAsync();
            if (!question.Result) return;

            try
            {
                _steamApi.InvalidateCache();
                UpdateServer();
                UpdateMods();
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal("Error", tex.Message).OpenDialogueAsync();
            }
        }

        private void OnKillAll()
        {
            foreach (var i in Instances)
                KillServer(i.Instance);
        }

        private void OnLaunchAll()
        {
            foreach(var i in Instances)
                LaunchServer(i.Instance);
        }
    }
}