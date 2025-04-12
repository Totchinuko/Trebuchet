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

namespace Trebuchet.ViewModels;

public sealed class ClientInstanceDashboard : ReactiveObject
{
    private readonly DialogueBox _box;
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

    public ClientInstanceDashboard(IProcessStats processStats, TaskBlocker blocker, DialogueBox box)
    {
        _box = box;
        ProcessStats = processStats;

        KillCommand = ReactiveCommand.CreateFromTask(OnKilled, this.WhenAnyValue(x => x.CanKill));
        CanKill = false;

        var canBlockerLaunch = blocker.WhenAnyValue(x => x.CanLaunch);
        var canDownloadMods = blocker.WhenAnyValue(x => x.CanDownloadMods);
        var canLaunch = this.WhenAnyValue(x => x.CanLaunch).CombineLatest(canBlockerLaunch, (f,s) => f && s);
        LaunchCommand = ReactiveCommand.CreateFromTask(OnLaunched, canLaunch);
        LaunchBattleEyeCommand = ReactiveCommand.CreateFromTask(OnBattleEyeLaunched, canLaunch);
        UpdateModsCommand = ReactiveCommand.CreateFromTask(OnModUpdate, canDownloadMods);

        this.WhenAnyValue(x => x.SelectedModlist)
            .InvokeCommand(ReactiveCommand.CreateFromTask<string>(OnModlistSelected));
        this.WhenAnyValue(x => x.SelectedProfile)
            .InvokeCommand(ReactiveCommand.CreateFromTask<string>(OnProfileSelected));
    }

    public event AsyncEventHandler? KillClicked;
    public event AsyncEventHandler<bool>? LaunchClicked;
    public event AsyncEventHandler<string>? ProfileSelected;
    public event AsyncEventHandler<string>? ModlistSelected;
    public event AsyncEventHandler? UpdateClicked;

    public ReactiveCommand<Unit,Unit> KillCommand { get; }
    public ReactiveCommand<Unit,Unit> LaunchBattleEyeCommand { get; }
    public ReactiveCommand<Unit,Unit> LaunchCommand { get; }
    public ReactiveCommand<Unit,Unit> UpdateModsCommand { get; }

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

    public IProcessStats ProcessStats { get; }

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
            await KillClicked.Invoke(this, EventArgs.Empty);
    }

    private async Task OnLaunchClicked(bool battleEye)
    {
        if (LaunchClicked is not null)
            await LaunchClicked.Invoke(this, battleEye);
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

    private async Task OnUpdateClicked()
    {
        if (UpdateClicked is not null)
            await UpdateClicked.Invoke(this, EventArgs.Empty);
    }

    private async Task OnBattleEyeLaunched()
    {
        CanLaunch = false;
        if(LaunchClicked is not null)
            await LaunchClicked.Invoke(this, true);
    }

    private async Task OnKilled()
    {
        CanKill = false;
        await OnKillClicked();
    }

    private async Task OnLaunched()
    {
        CanLaunch = false;
        await OnLaunchClicked(false);
    }

    private async Task OnModUpdate()
    {
        if (string.IsNullOrEmpty(SelectedModlist)) return;
        await OnUpdateClicked();
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