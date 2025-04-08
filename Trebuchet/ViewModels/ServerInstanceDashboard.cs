using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
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
    
    public sealed class ServerInstanceDashboard : ReactiveObject
    {
        private readonly TaskBlocker _blocker;
        private ProcessState _lastState;
        private bool _canClose;
        private bool _canKill;
        private bool _canLaunch;
        private bool _canUseDashboard;
        private List<string> _modlists = [];
        private bool _processRunning;
        private List<string> _profiles = [];
        private string _selectedModlist = string.Empty;
        private string _selectedProfile = string.Empty;
        private List<ulong> _updateNeeded = [];

        public ServerInstanceDashboard(int instance, IProcessStats stats, TaskBlocker blocker)
        {
            _blocker = blocker;
            Instance = instance;
            ProcessStats = stats;
            
            KillCommand = ReactiveCommand.Create(OnKilled, this.WhenAnyValue(x => x.CanKill));
            CanKill = false;
            
            CloseCommand = ReactiveCommand.Create(OnClose, this.WhenAnyValue(x => x.CanClose));
            CanClose = false;

            var canBlockLaunch = blocker.WhenAnyValue(x => x.CanLaunch);
            var canDownloadMods = blocker.WhenAnyValue(x => x.CanDownloadMods);
            var canLaunch = this.WhenAnyValue(x => x.CanLaunch).CombineLatest(canBlockLaunch, (f,s) => f && s);
            LaunchCommand = ReactiveCommand.Create(OnLaunched, canLaunch.StartWith(true));
            UpdateModsCommand = ReactiveCommand.Create(OnModUpdate, canDownloadMods);
            
            this.WhenAnyValue(x => x.SelectedModlist)
                .Subscribe((x) => ModlistSelected?.Invoke(this, new ServerInstanceSelectionEventArgs(Instance, x)));
            this.WhenAnyValue(x => x.SelectedProfile)
                .Subscribe((x) => ProfileSelected?.Invoke(this, new ServerInstanceSelectionEventArgs(Instance, x)));
        }

        public event EventHandler<int>? LaunchClicked;
        public event EventHandler<int>? KillClicked;
        public event EventHandler<int>? CloseClicked;
        public event EventHandler<ServerInstanceSelectionEventArgs>? ModlistSelected;
        public event EventHandler<ServerInstanceSelectionEventArgs>? ProfileSelected;

        public event EventHandler<int>? UpdateClicked;

        public bool CanClose
        {
            get => _canClose;
            set => this.RaiseAndSetIfChanged(ref _canClose, value);
        }

        public bool CanKill
        {
            get => _canKill;
            set => this.RaiseAndSetIfChanged(ref _canKill, value);
        }

        public bool CanLaunch
        {
            get => _canLaunch;
            set => this.RaiseAndSetIfChanged(ref _canLaunch, value);
        }

        public bool CanUseDashboard
        {
            get => _canUseDashboard;
            set => this.RaiseAndSetIfChanged(ref _canUseDashboard, value);
        }

        public List<string> Modlists
        {
            get => _modlists;
            set => this.RaiseAndSetIfChanged(ref _modlists, value);
        }

        public bool ProcessRunning
        {
            get => _processRunning;
            set => this.RaiseAndSetIfChanged(ref _processRunning, value);
        }

        public List<string> Profiles
        {
            get => _profiles;
            set => this.RaiseAndSetIfChanged(ref _profiles, value);
        }

        public string SelectedModlist
        {
            get => _selectedModlist;
            set => this.RaiseAndSetIfChanged(ref _selectedModlist, value);
        }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
        }

        public List<ulong> UpdateNeeded
        {
            get => _updateNeeded;
            set => this.RaiseAndSetIfChanged(ref _updateNeeded, value);
        }

        public IProcessStats ProcessStats { get; }

        public int Instance { get; }
        public ReactiveCommand<Unit,Unit> CloseCommand { get; }
        public ReactiveCommand<Unit,Unit> KillCommand { get; }
        public ReactiveCommand<Unit,Unit> LaunchCommand { get; }
        public ReactiveCommand<Unit,Unit> UpdateModsCommand { get; private set; }

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

        private void OnClose()
        {
            CanClose = false;
            CloseClicked?.Invoke(this, Instance);
        }

        private void OnKilled()
        {
            CanKill = false;
            KillClicked?.Invoke(this, Instance);
        }

        private void OnLaunched()
        {
            if (!CanUseDashboard) return;
            CanLaunch = false;
            LaunchClicked?.Invoke(this, Instance);
        }

        private void OnModUpdate()
        {
            if (string.IsNullOrEmpty(SelectedModlist)) return;
            UpdateClicked?.Invoke(this, Instance);
        }

        private async void OnProcessFailed()
        {
            CanKill = false;
            CanClose = false;
            CanLaunch = true;
            await new ErrorModal(Resources.ServerFailedStart, Resources.ServerFailedStartText).OpenDialogueAsync();
        }

        private void OnProcessStarted(IConanProcess details, bool refreshProcess)
        {
            CanLaunch = false;
            CanKill = true;
            CanClose = true;

            ProcessRunning = true;
            if(refreshProcess)
                ProcessStats.StartStats(details);
        }

        private void OnProcessTerminated()
        {
            ProcessStats.StopStats();
            CanKill = false;
            CanClose = false;
            CanLaunch = true;
            ProcessRunning = false;
        }
    }
}