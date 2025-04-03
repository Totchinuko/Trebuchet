using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Trebuchet.Services.TaskBlocker;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet.ViewModels;

public class ClientInstanceDashboard : BaseViewModel
{
    private ProcessState _lastState;
    private string _selectedModlist = string.Empty;
    private string _selectedProfile = string.Empty;
    private bool _processRunning;
    private List<string> _modlists = new();
    private List<string> _profiles = new();
    private List<ulong> _updateNeeded = new();
    private bool _canUseDashboard;

    public ClientInstanceDashboard(IProcessStats processStats)
    {
        ProcessStats = processStats;

        KillCommand.Subscribe(OnKilled).Toggle(false);
        LaunchCommand
            .SetBlockingType<SteamDownload>()
            .Subscribe(OnLaunched);
        LaunchBattleEyeCommand
            .SetBlockingType<SteamDownload>()
            .Subscribe(OnBattleEyeLaunched);
        UpdateModsCommand
            .SetBlockingType<SteamDownload>()
            .SetBlockingType<ClientRunning>()
            .SetBlockingType<ServersRunning>()
            .Subscribe(OnModUpdate);
    }

    public event EventHandler? KillClicked;
    public event EventHandler<bool>? LaunchClicked;
    public event EventHandler<string>? ProfileSelected;
    public event EventHandler<string>? ModlistSelected;

    public event EventHandler? UpdateClicked;

    public bool CanUseDashboard
    {
        get => _canUseDashboard;
        set => SetField(ref _canUseDashboard, value);
    }

    public SimpleCommand KillCommand { get; } = new();
    public TaskBlockedCommand LaunchBattleEyeCommand { get; } = new();
    public TaskBlockedCommand LaunchCommand { get; } = new();
    public TaskBlockedCommand UpdateModsCommand { get; private set; } = new();

    public List<string> Modlists
    {
        get => _modlists;
        set => SetField(ref _modlists, value);
    }

    public bool ProcessRunning
    {
        get => _processRunning;
        set => SetField(ref _processRunning, value);
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
                ModlistSelected?.Invoke(this, value);
        }
    }

    public string SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if(SetField(ref _selectedProfile, value))
                ProfileSelected?.Invoke(this, value);
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

    private void OnBattleEyeLaunched()
    {
        LaunchClicked?.Invoke(this, true);
    }

    private void OnKilled()
    {
        KillClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnLaunched()
    {
        LaunchClicked?.Invoke(this, false);
    }

    private void OnModUpdate()
    {
        if (string.IsNullOrEmpty(SelectedModlist)) return;
        UpdateClicked?.Invoke(this, EventArgs.Empty);
    }

    private async void OnProcessFailed()
    {
        KillCommand.Toggle(false);
        LaunchCommand.Toggle(true);
        LaunchBattleEyeCommand.Toggle(true);
        await new ErrorModal("Client failed to start", "See the logs for more information.").OpenDialogueAsync();
    }

    private void OnProcessStarted(IConanProcess details, bool refreshStats)
    {
        LaunchCommand.Toggle(false);
        LaunchBattleEyeCommand.Toggle(false);
        KillCommand.Toggle(true);

        ProcessRunning = true;

        if(refreshStats)
            ProcessStats.StartStats(details);
    }

    private void OnProcessTerminated()
    {
        ProcessStats.StopStats();
        KillCommand.Toggle(false);
        LaunchCommand.Toggle(true);
        LaunchBattleEyeCommand.Toggle(true);

        ProcessRunning = false;
    }
}