using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Processes;

namespace Trebuchet.ViewModels;

public class ClientInstanceDashboard : ReactiveObject
{
    private readonly DialogueBox _box;
    private ProcessState _lastState;
    private bool _canKill;
    private bool _canLaunch;
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

        KillCommand = ReactiveCommand.Create(OnKilled, this.WhenAnyValue(x => x.CanKill));
        CanKill = false;

        var canBlockerLaunch = blocker.WhenAnyValue(x => x.CanLaunch);
        var canDownloadMods = blocker.WhenAnyValue(x => x.CanDownloadMods);
        var canLaunch = this.WhenAnyValue(x => x.CanLaunch).CombineLatest(canBlockerLaunch, (f,s) => f && s);
        LaunchCommand = ReactiveCommand.Create(OnLaunched, canLaunch.StartWith(true));
        LaunchBattleEyeCommand = ReactiveCommand.Create(OnBattleEyeLaunched, canLaunch.StartWith(true));
        UpdateModsCommand = ReactiveCommand.Create(OnModUpdate, canDownloadMods);

        this.WhenAnyValue(x => x.SelectedModlist)
            .Subscribe((x) => ModlistSelected?.Invoke(this, x));
        this.WhenAnyValue(x => x.SelectedProfile)
            .Subscribe((x) => ProfileSelected?.Invoke(this, x));
    }

    public event EventHandler? KillClicked;
    public event EventHandler<bool>? LaunchClicked;
    public event EventHandler<string>? ProfileSelected;
    public event EventHandler<string>? ModlistSelected;

    public event EventHandler? UpdateClicked;

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

    private void OnBattleEyeLaunched()
    {
        CanLaunch = false;
        LaunchClicked?.Invoke(this, true);
    }

    private void OnKilled()
    {
        CanKill = false;
        KillClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnLaunched()
    {
        CanLaunch = false;
        LaunchClicked?.Invoke(this, false);
    }

    private void OnModUpdate()
    {
        if (string.IsNullOrEmpty(SelectedModlist)) return;
        UpdateClicked?.Invoke(this, EventArgs.Empty);
    }

    private async void OnProcessFailed()
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
            ProcessStats.StartStats(details);
    }

    private void OnProcessTerminated()
    {
        CanKill = false;
        CanLaunch = true;

        ProcessRunning = false;
    }
}