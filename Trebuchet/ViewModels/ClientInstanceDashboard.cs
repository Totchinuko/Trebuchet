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


    public ClientInstanceDashboard(IProcessStats processStats, TaskBlocker blocker, DialogueBox box)
    {
        _box = box;
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
        ConnectTo = ReactiveCommand.CreateFromTask<ClientConnectionRefViewModel>(OnConnect, canConnect);

        this.WhenAnyValue(x => x.SelectedModlist)
            .InvokeCommand(ReactiveCommand.CreateFromTask<ModListRefViewModel?>(OnModlistSelected));
        this.WhenAnyValue(x => x.SelectedProfile)
            .InvokeCommand(ReactiveCommand.CreateFromTask<ClientProfileRef?>(OnProfileSelected));
        this.WhenAnyValue(x => x.BattleEye)
            .InvokeCommand(ReactiveCommand.CreateFromTask<bool>(OnBattleEyeChanged));
    }
    
    private readonly DialogueBox _box;
    private ProcessState _lastState;
    private bool _canKill;
    private bool _canLaunch = true;
    private List<ModListRefViewModel> _modlists = [];
    private bool _processRunning;
    private List<ClientProfileRef> _profiles = [];
    private ModListRefViewModel? _selectedModlist;
    private ClientProfileRef? _selectedProfile;
    private List<ulong> _updateNeeded = [];
    private bool _canUseDashboard;
    private bool _battleEye;
    private bool _connectPopupOpen;
    private bool _canConnect;

    public event AsyncEventHandler? KillClicked;
    public event AsyncEventHandler<ClientConnectionRef?>? LaunchClicked;
    public event AsyncEventHandler<ClientProfileRef>? ProfileSelected;
    public event AsyncEventHandler<IPRefWithModList>? ModlistSelected;
    public event AsyncEventHandler<bool>? BattleEyeChanged; 
    public event AsyncEventHandler? UpdateClicked;

    public ReactiveCommand<Unit,Unit> KillCommand { get; }
    public ReactiveCommand<Unit,Unit> LaunchAndConnect { get; }
    public ReactiveCommand<Unit,Unit> LaunchCommand { get; }
    public ReactiveCommand<Unit,Unit> UpdateModsCommand { get; }
    public ReactiveCommand<ClientConnectionRefViewModel, Unit> ConnectTo { get; }

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

    public List<ModListRefViewModel> Modlists
    {
        get => _modlists;
        set => this.RaiseAndSetIfChanged(ref _modlists, value);
    }

    public bool ProcessRunning
    {
        get => _processRunning;
        set => this.RaiseAndSetIfChanged(ref _processRunning, value);
    }

    public List<ClientProfileRef> Profiles
    {
        get => _profiles;
        set => this.RaiseAndSetIfChanged(ref _profiles, value);
    }

    public ModListRefViewModel? SelectedModlist
    {
        get => _selectedModlist;
        set => this.RaiseAndSetIfChanged(ref _selectedModlist, value);
    }

    public ClientProfileRef? SelectedProfile
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

    public ObservableCollectionExtended<ClientConnectionRefViewModel> ConnectionList { get; } = [];

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

    private async Task OnProfileSelected(ClientProfileRef? profile)
    {
        if (ProfileSelected is not null && profile is not null)
            await ProfileSelected.Invoke(this, profile);
    }

    private async Task OnModlistSelected(ModListRefViewModel? list)
    {
        if (ModlistSelected is not null && list is not null)
            await ModlistSelected.Invoke(this, list.ModList);
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
        {
            if(LaunchClicked is not null)
                await LaunchClicked.Invoke(this, ConnectionList[0].Reference);
        }
        else
            ConnectPopupOpen = true;
    }

    private async Task OnConnect(ClientConnectionRefViewModel autoConnect)
    {
        ConnectPopupOpen = false;
        if(LaunchClicked is not null)
            await LaunchClicked.Invoke(this, autoConnect.Reference);
    }

    private void RefreshConnectionList()
    {
        using (ConnectionList.SuspendNotifications())
        {
            ConnectionList.Clear();
            if(SelectedProfile is not null)
                ConnectionList.AddRange(SelectedProfile
                    .GetConnectionRefs()
                    .Select(x => new ClientConnectionRefViewModel(x, Resources.Save)));
            if(SelectedModlist?.ModList is IPRefWithClientConnection collection)
                ConnectionList.AddRange(collection
                    .GetConnectionRefs()
                    .Select(x => new ClientConnectionRefViewModel(x, Resources.Sync)));
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
            await LaunchClicked.Invoke(this, null);
    }

    private async Task OnModUpdate()
    {
        if (SelectedModlist is null) return;
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