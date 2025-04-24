using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using ReactiveUI;
using tot_lib;
using Trebuchet.Assets;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels;

public sealed class ClientInstanceDashboard : ReactiveObject
{


    public ClientInstanceDashboard(IProcessStats processStats, TaskBlocker blocker, DialogueBox box, AppFiles files)
    {
        _box = box;
        _files = files;
        ProcessStats = processStats;

        KillCommand = ReactiveCommand.CreateFromTask(OnKilled, this.WhenAnyValue(x => x.CanKill));
        CanKill = false;
        RefreshConnectionList();

        var canBlockerLaunch = blocker.WhenAnyValue(x => x.CanLaunch);
        var canDownloadMods = blocker.WhenAnyValue(x => x.CanDownloadMods);
        var canLaunch = this.WhenAnyValue(x => x.CanLaunch).CombineLatest(canBlockerLaunch, (f,s) => f && s);
        var canConnect = this.WhenAnyValue(x => x.CanLaunch, x => x.CanConnect, (l, c) => l && c)
            .CombineLatest(canBlockerLaunch, (f, s) => f && s);
        LaunchCommand = ReactiveCommand.CreateFromTask(OnLaunched, canLaunch);
        LaunchAndConnect = ReactiveCommand.CreateFromTask(OnConnect, canLaunch);
        UpdateModsCommand = ReactiveCommand.CreateFromTask(OnModUpdate, canDownloadMods);
        ConnectTo = ReactiveCommand.CreateFromTask<string>(OnConnect, canConnect);

        this.WhenAnyValue(x => x.SelectedModlist)
            .InvokeCommand(ReactiveCommand.CreateFromTask<string>(OnModlistSelected));
        this.WhenAnyValue(x => x.SelectedProfile)
            .InvokeCommand(ReactiveCommand.CreateFromTask<string>(OnProfileSelected));
        this.WhenAnyValue(x => x.BattleEye)
            .InvokeCommand(ReactiveCommand.CreateFromTask<bool>(OnBattleEyeChanged));
    }
    
    private readonly DialogueBox _box;
    private readonly AppFiles _files;
    private ProcessState _lastState;
    private bool _canKill;
    private bool _canLaunch = true;
    private List<string> _modlists = [];
    private bool _processRunning;
    private List<string> _profiles = [];
    private string _selectedModlist = string.Empty;
    private string _selectedProfile = string.Empty;
    private List<ulong> _updateNeeded = [];
    private bool _canUseDashboard;
    private bool _battleEye;
    private bool _connectPopupOpen;
    private bool _canConnect;

    public event AsyncEventHandler? KillClicked;
    public event AsyncEventHandler<string>? LaunchClicked;
    public event AsyncEventHandler<string>? ProfileSelected;
    public event AsyncEventHandler<string>? ModlistSelected;
    public event AsyncEventHandler<bool>? BattleEyeChanged; 
    public event AsyncEventHandler? UpdateClicked;

    public ReactiveCommand<Unit,Unit> KillCommand { get; }
    public ReactiveCommand<Unit,Unit> LaunchAndConnect { get; }
    public ReactiveCommand<Unit,Unit> LaunchCommand { get; }
    public ReactiveCommand<Unit,Unit> UpdateModsCommand { get; }
    public ReactiveCommand<string, Unit> ConnectTo { get; }

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

    public bool BattleEye
    {
        get => _battleEye;
        set => this.RaiseAndSetIfChanged(ref _battleEye, value);
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

    public bool CanUseDashboard
    {
        get => _canUseDashboard;
        set => this.RaiseAndSetIfChanged(ref _canUseDashboard, value);
    }

    public bool ConnectPopupOpen
    {
        get => _connectPopupOpen;
        set => this.RaiseAndSetIfChanged(ref _connectPopupOpen, value);
    }

    public bool CanConnect
    {
        get => _canConnect;
        set => this.RaiseAndSetIfChanged(ref _canConnect, value);
    }

    public IProcessStats ProcessStats { get; }

    public ObservableCollectionExtended<string> ConnectionList { get; } = [];

    public void RefreshPanel()
    {
        RefreshConnectionList();
    }

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

    private async Task OnProfileSelected(string profile)
    {
        if (ProfileSelected is not null)
            await ProfileSelected.Invoke(this, profile);
    }

    private async Task OnModlistSelected(string modlist)
    {
        if (ModlistSelected is not null)
            await ModlistSelected.Invoke(this, modlist);
    }

    private async Task OnBattleEyeChanged(bool battleEye)
    {
        if(BattleEyeChanged is not null)
            await BattleEyeChanged.Invoke(this, battleEye);
    }

    private async Task OnConnect()
    {
        RefreshConnectionList();
        if (ConnectionList.Count == 1)
            await OnConnect(ConnectionList[0]);
        else
            ConnectPopupOpen = true;
    }

    private async Task OnConnect(string autoConnect)
    {
        ConnectPopupOpen = false;
        if(LaunchClicked is not null)
            await LaunchClicked.Invoke(this, autoConnect);
    }

    private void RefreshConnectionList()
    {
        using (ConnectionList.SuspendNotifications())
        {
            ConnectionList.Clear();
            ConnectionList.AddRange(_files.Client.Get(SelectedProfile).ClientConnections.Select(x => x.Name));
        }

        CanConnect = ConnectionList.Count > 0;
    }

    private async Task OnKilled()
    {
        if (KillClicked is not null)
            await KillClicked.Invoke(this, EventArgs.Empty);
    }

    private async Task OnLaunched()
    {
        if(LaunchClicked is not null)
            await LaunchClicked.Invoke(this, string.Empty);
    }

    private async Task OnModUpdate()
    {
        if (string.IsNullOrEmpty(SelectedModlist)) return;
        if (UpdateClicked is not null)
            await UpdateClicked.Invoke(this, EventArgs.Empty);
    }

    private async Task OnProcessFailed()
    {
        CanKill = false;
        CanLaunch = true;
        await _box.OpenErrorAsync(Resources.ClientFailedToStart, Resources.ClientFailedToStartText);
    }

    private void OnProcessStarted(IConanProcess details, bool refreshStats)
    {
        CanLaunch = false;
        CanKill = true;
        ProcessRunning = true;

        if(refreshStats)
            ProcessStats.Details = details;
    }

    private void OnProcessTerminated()
    {
        ProcessStats.Details = ConanProcess.Empty;
        CanKill = false;
        CanLaunch = true;

        ProcessRunning = false;
    }
}