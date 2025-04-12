using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using tot_lib;
using Trebuchet.Assets;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Processes;

namespace Trebuchet.ViewModels
{
    public class ServerInstanceSelectionEventArgs(int instance, string selection) : EventArgs
    {
        public int Instance { get; } = instance;
        public string Selection { get; } = selection;
    }
    
    public sealed class ServerInstanceDashboard : ReactiveObject
    {
        private readonly DialogueBox _box;
        private ProcessState _lastState;
        private bool _canClose;
        private bool _canKill;
        private bool _canLaunch = true;
        private bool _canUseDashboard;
        private List<string> _modlists = [];
        private bool _processRunning;
        private List<string> _profiles = [];
        private string _selectedModlist = string.Empty;
        private string _selectedProfile = string.Empty;
        private List<ulong> _updateNeeded = [];

        public ServerInstanceDashboard(int instance, IProcessStats stats, TaskBlocker blocker, DialogueBox box)
        {
            _box = box;
            Instance = instance;
            ProcessStats = stats;
            
            KillCommand = ReactiveCommand.CreateFromTask(OnKilled, this.WhenAnyValue(x => x.CanKill));
            CanKill = false;
            
            CloseCommand = ReactiveCommand.Create(OnClose, this.WhenAnyValue(x => x.CanClose));
            CanClose = false;

            var canBlockLaunch = blocker.WhenAnyValue(x => x.CanLaunch);
            var canDownloadMods = blocker.WhenAnyValue(x => x.CanDownloadMods);
            var canLaunch = this.WhenAnyValue(x => x.CanLaunch).CombineLatest(canBlockLaunch, (f,s) => f && s);
            LaunchCommand = ReactiveCommand.CreateFromTask(OnLaunched, canLaunch.StartWith(true));
            UpdateModsCommand = ReactiveCommand.CreateFromTask(OnModUpdate, canDownloadMods);

            this.WhenAnyValue(x => x.SelectedModlist)
                .InvokeCommand(ReactiveCommand.CreateFromTask<string>(OnModlistSelected));
            this.WhenAnyValue(x => x.SelectedProfile)
                .InvokeCommand(ReactiveCommand.CreateFromTask<string>(OnProfileSelected));
        }

        public event AsyncEventHandler<int>? LaunchClicked;
        public event AsyncEventHandler<int>? KillClicked;
        public event AsyncEventHandler<int>? CloseClicked;
        public event AsyncEventHandler<ServerInstanceSelectionEventArgs>? ModlistSelected;
        public event AsyncEventHandler<ServerInstanceSelectionEventArgs>? ProfileSelected;
        public event AsyncEventHandler<int>? UpdateClicked;

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

        public async Task ProcessRefresh(IConanProcess? process, bool refreshStats)
        {
            var state = process?.State ?? ProcessState.STOPPED;
            if (_lastState.IsRunning() && !state.IsRunning())
                OnProcessTerminated();
            else if (!_lastState.IsRunning() && state.IsRunning() && process is not null)
                OnProcessStarted(process, refreshStats);

            if (state == ProcessState.FAILED)
                await OnProcessFailed();
            else if (ProcessRunning && process is not null)
                ProcessStats.Details = process;

            _lastState = state;
        }
        
        private async Task OnKillClicked()
        {
            if (KillClicked is not null)
                await KillClicked.Invoke(this, Instance);
        }

        private async Task OnLaunchClicked()
        {
            if (LaunchClicked is not null)
                await LaunchClicked.Invoke(this, Instance);
        }

        private async Task OnProfileSelected(string profile)
        {
            if (ProfileSelected is not null)
                await ProfileSelected.Invoke(this, new ServerInstanceSelectionEventArgs(Instance, profile));
        }

        private async Task OnModlistSelected(string modlist)
        {
            if (ModlistSelected is not null)
                await ModlistSelected.Invoke(this, new ServerInstanceSelectionEventArgs(Instance, modlist));
        }

        private async Task OnUpdateClicked()
        {
            if (UpdateClicked is not null)
                await UpdateClicked.Invoke(this, Instance);
        }

        private void OnClose()
        {
            CanClose = false;
            CloseClicked?.Invoke(this, Instance);
        }

        private async Task OnKilled()
        {
            CanKill = false;
            await OnKillClicked();
        }

        private async Task OnLaunched()
        {
            if (!CanUseDashboard) return;
            CanLaunch = false;
            await OnLaunchClicked();
        }

        private async Task OnModUpdate()
        {
            if (string.IsNullOrEmpty(SelectedModlist)) return;
            await OnUpdateClicked();
        }

        private async Task OnProcessFailed()
        {
            CanKill = false;
            CanClose = false;
            CanLaunch = true;
            await _box.OpenErrorAsync(Resources.ServerFailedStart, Resources.ServerFailedStartText);
        }

        private void OnProcessStarted(IConanProcess details, bool refreshProcess)
        {
            CanLaunch = false;
            CanKill = true;
            CanClose = true;

            ProcessRunning = true;
            if(refreshProcess)
                ProcessStats.Details = details;
        }

        private void OnProcessTerminated()
        {
            ProcessStats.Details = ConanProcess.Empty;
            CanKill = false;
            CanClose = false;
            CanLaunch = true;
            ProcessRunning = false;
        }
    }
}