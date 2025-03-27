using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Trebuchet.Messages;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetLib.Services;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet
{
    public sealed class ServerInstanceDashboard : INotifyPropertyChanged
    {
        private readonly UIConfig _uiConfig;
        private readonly AppFiles _appFiles;
        private readonly SteamAPI _steamApi;
        private readonly Launcher _launcher;
        private readonly ILogger _logger;
        private ProcessState _lastState;
        private string _selectedModlist;
        private string _selectedProfile;
        private bool _processRunning;
        private List<ulong> _updateNeeded = [];
        private List<string> _modlists = [];
        private List<string> _profiles = [];

        public ServerInstanceDashboard(
            int instance,
            UIConfig uiConfig,
            AppFiles appFiles,
            SteamAPI steamApi,
            Launcher launcher,
            AppSetup setup,
            ILogger logger)
        {
            _uiConfig = uiConfig;
            _appFiles = appFiles;
            _steamApi = steamApi;
            _launcher = launcher;
            _logger = logger;
            Instance = instance;
            
            KillCommand = new SimpleCommand(OnKilled, false);
            CloseCommand = new SimpleCommand(OnClose, false);
            LaunchCommand = new TaskBlockedCommand(OnLaunched)
                .SetBlockingType<SteamDownload>();
            UpdateModsCommand = new TaskBlockedCommand(OnModUpdate)
                .SetBlockingType<SteamDownload>()
                .SetBlockingType<ClientRunning>()
                .SetBlockingType<ServersRunning>();

            uiConfig.GetInstanceParameters(Instance, out _selectedModlist, out _selectedProfile);

            Resolve();
            ListProfiles();

            if (setup.Catapult)
                Launch();
        }

        public bool CanUseDashboard => _config.IsInstallPathValid &&
                (_config.ServerInstanceCount > Instance || ProcessRunning);

        public SimpleCommand CloseCommand { get; private set; }

        public int Instance { get; }

        public bool IsUpdateNeeded => UpdateNeeded.Count > 0;

        public SimpleCommand KillCommand { get; private set; }

        public TaskBlockedCommand LaunchCommand { get; private set; }

        public List<string> Modlists
        {
            get => _modlists;
            private set => SetField(ref _modlists, value);
        }

        public bool ProcessRunning
        {
            get => _processRunning;
            private set
            {
                if (SetField(ref _processRunning, value)) OnPropertyChanged(nameof(CanUseDashboard));
            }
        }

        public IProcessStats ProcessStats { get; } = new ProcessStatsLight();

        public List<string> Profiles
        {
            get => _profiles;
            private set => SetField(ref _profiles, value);
        }

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                CheckModUpdate();
                _uiConfig.SetInstanceParameters(Instance, _selectedModlist, _selectedProfile);
                _uiConfig.SaveFile();
            }
        }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                _uiConfig.SetInstanceParameters(Instance, _selectedModlist, _selectedProfile);
                _uiConfig.SaveFile();
            }
        }

        public TaskBlockedCommand UpdateModsCommand { get; private set; }

        public List<ulong> UpdateNeeded
        {
            get => _updateNeeded;
            private set => SetField(ref _updateNeeded, value);
        }

        public void Close()
        {
            if (!ProcessRunning) return;

            CloseCommand.Toggle(false);
            StrongReferenceMessenger.Default.Send(new CloseProcessMessage(Instance));
        }

        public async void Kill()
        {
            if (!ProcessRunning) return;

            if (_uiConfig.DisplayWarningOnKill)
            {
                QuestionModal question = new QuestionModal(App.GetAppText("Kill_Title"), App.GetAppText("Kill_Message"));
                await question.OpenDialogueAsync();
                if (!question.Result) return;
            }

            KillCommand.Toggle(false);
            CloseCommand.Toggle(false);
            StrongReferenceMessenger.Default.Send(new KillProcessMessage(Instance));
        }

        public void RefreshSelection()
        {
            Resolve();
            ListProfiles();
        }

        public void RefreshUpdateStatus(List<ulong> queried, List<ulong> updated)
        {
            if (CanUseDashboard && !string.IsNullOrEmpty(SelectedModlist))
            {
                var mods = _appFiles.Mods.CollectAllMods(SelectedModlist);
                UpdateNeeded = mods.Intersect(updated).Union(UpdateNeeded.Except(queried)).ToList();
            }
        }

        public void ProcessRefresh(IConanProcess? process)
        {
            var state = process?.State ?? ProcessState.STOPPED;
            if (_lastState.IsRunning() && !state.IsRunning())
                OnProcessTerminated();
            else if (!_lastState.IsRunning() && state.IsRunning() && process is not null)
                OnProcessStarted(process);

            if (state == ProcessState.FAILED)
                OnProcessFailed();
            else if (ProcessRunning && process is not null)
                ProcessStats.SetDetails(process);

            _lastState = state;
        }

        public async void Launch()
        {
            if(ProcessRunning) return;
            LaunchCommand.Toggle(false);

            try
            {
                if (_config.AutoUpdateStatus != AutoUpdateStatus.Never && !_launcher.IsAnyServerRunning() &&
                    !_launcher.IsClientRunning())
                {
                    var modlist = _appFiles.Mods.CollectAllMods(SelectedModlist).ToList();
                    await _steamApi.UpdateServers();
                    await _steamApi.UpdateMods(modlist);
                }

                await _launcher.CatapultServer(SelectedProfile, SelectedModlist, Instance);
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal("Error", tex.Message).OpenDialogueAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                LaunchCommand.Toggle(true);
            }
        }

        private void CheckModUpdate()
        {
            ClearUpdates();
            if (string.IsNullOrEmpty(SelectedModlist)) return;
            
        }

        private void ClearUpdates()
        {
            UpdateNeeded.Clear();
        }

        private void ListProfiles()
        {
            Modlists = _appFiles.Mods.ListProfiles().ToList();
            Profiles = _appFiles.Server.ListProfiles().ToList();
        }

        private void OnClose(object? obj)
        {
            Close();
        }

        private void OnKilled(object? obj)
        {
            Kill();
        }

        private void OnLaunched(object? obj)
        {
            if (!CanUseDashboard) return;

            Launch();
        }

        private async void OnModUpdate(object? obj)
        {
            if (string.IsNullOrEmpty(SelectedModlist)) return;
            await UpdateMods();
        }

        public async Task UpdateMods()
        {
            try
            {
                var modlist = _appFiles.Mods.CollectAllMods(SelectedModlist).ToList();
                await _steamApi.UpdateMods(modlist);
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal("Error", tex.Message).OpenDialogueAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private async void OnProcessFailed()
        {
            KillCommand.Toggle(false);
            CloseCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            await new ErrorModal(App.GetAppText("ServerFailedStart"), "ServerFailedStart_Message").OpenDialogueAsync();
        }

        private void OnProcessStarted(IConanProcess details)
        {
            LaunchCommand.Toggle(false);
            KillCommand.Toggle(true);
            CloseCommand.Toggle(true);

            ProcessRunning = true;
            ProcessStats.StartStats(details);
            StrongReferenceMessenger.Default.Send<DashboardStateChanged>();
        }

        private void OnProcessTerminated()
        {
            ProcessStats.StopStats();
            KillCommand.Toggle(false);
            CloseCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            ProcessRunning = false;
            StrongReferenceMessenger.Default.Send<DashboardStateChanged>();
        }

        private void Resolve()
        {
            var profile = _appFiles.Server.ResolveProfile(_selectedProfile);
            var modlist = _appFiles.Mods.ResolveProfile(_selectedModlist);

            _uiConfig.SetInstanceParameters(Instance, _selectedModlist, _selectedProfile);
            _uiConfig.SaveFile();
            SelectedProfile = profile;
            SelectedModlist = modlist;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}