using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Trebuchet.Services.TaskBlocker;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet.ViewModels;

public class ClientInstanceDashboard : INotifyPropertyChanged
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

        KillCommand = new SimpleCommand(OnKilled, false);
        LaunchCommand = new TaskBlockedCommand(OnLaunched)
            .SetBlockingType<SteamDownload>();
        LaunchBattleEyeCommand = new TaskBlockedCommand(OnBattleEyeLaunched)
            .SetBlockingType<SteamDownload>();
        UpdateModsCommand = new TaskBlockedCommand(OnModUpdate)
            .SetBlockingType<SteamDownload>()
            .SetBlockingType<ClientRunning>()
            .SetBlockingType<ServersRunning>();
    }

    public event EventHandler? KillClicked;
    public event EventHandler<bool>? LaunchClicked;
    public event EventHandler<string>? ProfileSelected;
    public event EventHandler<string>? ModlistSelected;

    public event EventHandler? UpdateClicked;

    // public bool CanUseDashboard => _setup.Config.IsInstallPathValid &&
    //                                !string.IsNullOrEmpty(_setup.Config.ClientPath) &&
    //                                File.Exists(Path.Combine(_setup.Config.ClientPath, Constants.FolderGameBinaries,
    //                                    Constants.FileClientBin));

    public bool CanUseDashboard
    {
        get => _canUseDashboard;
        set => SetField(ref _canUseDashboard, value);
    }

    public SimpleCommand KillCommand { get; }

    public TaskBlockedCommand LaunchBattleEyeCommand { get; }

    public TaskBlockedCommand LaunchCommand { get; }

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

    public TaskBlockedCommand UpdateModsCommand { get; private set; }

    public List<ulong> UpdateNeeded
    {
        get => _updateNeeded;
        set => SetField(ref _updateNeeded, value);
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
        ProcessStats.Tick();
    }

    private void OnBattleEyeLaunched(object? obj)
    {
        LaunchClicked?.Invoke(this, true);
    }

    private void OnKilled(object? obj)
    {
        KillClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnLaunched(object? obj)
    {
        LaunchClicked?.Invoke(this, false);
    }

    private void OnModUpdate(object? obj)
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

    private void OnProcessStarted(IConanProcess details)
    {
        LaunchCommand.Toggle(false);
        LaunchBattleEyeCommand.Toggle(false);
        KillCommand.Toggle(true);

        ProcessRunning = true;

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}