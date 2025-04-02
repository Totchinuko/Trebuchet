using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trebuchet.Assets;
using Trebuchet.Services.TaskBlocker;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet.ViewModels
{
    public class ServerInstanceSelectionEventArgs(int instance, string selection) : EventArgs
    {
        public int Instance { get; } = instance;
        public string Selection { get; } = selection;
    }
    
    public sealed class ServerInstanceDashboard : BaseViewModel
    {
        private ProcessState _lastState;
        private string _selectedModlist = string.Empty;
        private string _selectedProfile = string.Empty;
        private bool _processRunning;
        private List<ulong> _updateNeeded = [];
        private List<string> _modlists = [];
        private List<string> _profiles = [];
        private bool _canUseDashboard;

        public ServerInstanceDashboard(int instance, IProcessStats stats)
        {
            Instance = instance;
            ProcessStats = stats;
            
            KillCommand = new SimpleCommand()
                .Toggle(false)
                .Subscribe(OnKilled);
            CloseCommand = new SimpleCommand()
                .Toggle(false)
                .Subscribe(OnClose);
            LaunchCommand = new TaskBlockedCommand()
                .SetBlockingType<SteamDownload>()
                .Subscribe(OnLaunched);
            UpdateModsCommand = new TaskBlockedCommand()
                .SetBlockingType<SteamDownload>()
                .SetBlockingType<ClientRunning>()
                .SetBlockingType<ServersRunning>()
                .Subscribe(OnModUpdate);
        }

        public event EventHandler<int>? LaunchClicked;
        public event EventHandler<int>? KillClicked;
        public event EventHandler<int>? CloseClicked;
        public event EventHandler<ServerInstanceSelectionEventArgs>? ModlistSelected;
        public event EventHandler<ServerInstanceSelectionEventArgs>? ProfileSelected;

        public event EventHandler<int>? UpdateClicked;

        public bool CanUseDashboard
        {
            get => _canUseDashboard;
            set => SetField(ref _canUseDashboard, value);
        }

        public int Instance { get; }
        public bool IsUpdateNeeded => UpdateNeeded.Count > 0;
        public SimpleCommand CloseCommand { get; }
        public SimpleCommand KillCommand { get; }
        public SimpleCommand LaunchCommand { get; }
        public SimpleCommand UpdateModsCommand { get; private set; }

        public List<string> Modlists
        {
            get => _modlists;
            set => SetField(ref _modlists, value);
        }

        public bool ProcessRunning
        {
            get => _processRunning;
            set
            {
                if (SetField(ref _processRunning, value)) OnPropertyChanged(nameof(CanUseDashboard));
            }
        }

        public IProcessStats ProcessStats { get; }

        public List<string> Profiles
        {
            get => _profiles;
            set => SetField(ref _profiles, value);
        }

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                if(SetField(ref _selectedModlist, value))
                    ModlistSelected?.Invoke(this, new ServerInstanceSelectionEventArgs(Instance, value));
            }
        }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if(SetField(ref _selectedProfile, value))
                    ProfileSelected?.Invoke(this, new ServerInstanceSelectionEventArgs(Instance, value));
            }
        }

        

        public List<ulong> UpdateNeeded
        {
            get => _updateNeeded;
            set => SetField(ref _updateNeeded, value);
        }

        public Task ProcessRefresh(IConanProcess? process, bool refreshStats)
        {
            var state = process?.State ?? ProcessState.STOPPED;
            if (_lastState.IsRunning() && !state.IsRunning())
                OnProcessTerminated();
            else if (!_lastState.IsRunning() && state.IsRunning() && process is not null)
                OnProcessStarted(process, refreshStats);

            if (state == ProcessState.FAILED)
                OnProcessFailed();
            else if (ProcessRunning && process is not null)
                ProcessStats.SetDetails(process);

            _lastState = state;
            return Task.CompletedTask;
        }

        private void OnClose(object? obj)
        {
            CloseClicked?.Invoke(this, Instance);
        }

        private void OnKilled(object? obj)
        {
            KillClicked?.Invoke(this, Instance);
        }

        private void OnLaunched(object? obj)
        {
            if (!CanUseDashboard) return;
            LaunchClicked?.Invoke(this, Instance);
        }

        private void OnModUpdate(object? obj)
        {
            if (string.IsNullOrEmpty(SelectedModlist)) return;
            UpdateClicked?.Invoke(this, Instance);
        }

        private async void OnProcessFailed()
        {
            KillCommand.Toggle(false);
            CloseCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            await new ErrorModal(Resources.ServerFailedStart, Resources.ServerFailedStartText).OpenDialogueAsync();
        }

        private void OnProcessStarted(IConanProcess details, bool refreshProcess)
        {
            LaunchCommand.Toggle(false);
            KillCommand.Toggle(true);
            CloseCommand.Toggle(true);

            ProcessRunning = true;
            if(refreshProcess)
                ProcessStats.StartStats(details);
        }

        private void OnProcessTerminated()
        {
            ProcessStats.StopStats();
            KillCommand.Toggle(false);
            CloseCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            ProcessRunning = false;
        }
    }
}