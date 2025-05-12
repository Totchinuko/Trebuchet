using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TrebuchetLib.Services;

namespace TrebuchetLib.Processes;

internal sealed class ConanServerProcess : IConanServerProcess
{
    public ConanServerProcess(Process process, LogReader logReader, UserDefinedNotifications notifications)
    {
        Process = process;
        _logReader = logReader;
        _notifications = notifications;
        PId = Process.Id;
        State = ProcessState.RUNNING;
        _lastResponse = DateTime.UtcNow;
    }

    private DateTime _lastResponse;
    private DateTime _shutdownCallTime;
    private int _maxPlayers;
    private bool _online;
    private int _players;
    private bool _onlineNotified;
    private bool _crashNotified;
    private ProcessState _state;
    private LogReader _logReader;
    private readonly UserDefinedNotifications _notifications;
    private bool _hasTriedRConShutdown;
    private bool _hasTriedHandleShutdown;

    public event EventHandler<ProcessState>? StateChanged; 
    public event PropertyChangedEventHandler? PropertyChanged;

    public long MemoryUsage
    {
        get
        {
            if (Process.HasExited)
                return 0;
            return Process.WorkingSet64;
        }
    }

    public TimeSpan CpuTime
    {
        get
        {
            if(Process.HasExited)
                return TimeSpan.Zero;
            return Process.TotalProcessorTime;
        }
    }
    
    public bool KillZombies { get; set; }
    public int ZombieCheckSeconds { get; set; }
    public int PId { get; }
    public DateTime StartUtc { get; init; }

    public ProcessState State
    {
        get => _state;
        private set
        {
            if (SetField(ref _state, value))
                OnStateChanged(_state);
        }
    }

    public int MaxPlayers
    {
        get => _maxPlayers;
        private set => SetField(ref _maxPlayers, value);
    }

    public int Players
    {
        get => _players;
        private set => SetField(ref _players, value);
    }

    public bool Online
    {
        get => _online;
        private set => SetField(ref _online, value);
    }

    public Process Process { get; }
    public required ConanServerInfos ServerInfos { get; init; }
    public required SourceQueryReader SourceQueryReader { get; init; }
    public IRcon? RCon { get; init; }
    public List<INotifier> Notifiers { get; init; } = [];

    public int Instance => ServerInfos.Instance;
    public int Port => ServerInfos.Port;
    public int QueryPort => ServerInfos.QueryPort;
    public string RConPassword => ServerInfos.RConPassword;
    public int RConPort => ServerInfos.RConPort;
    public string Title => ServerInfos.Title;

    public void Dispose()
    {
        Process.Dispose();
        SourceQueryReader.Dispose();
        _logReader.Dispose();
    }

    public async Task RefreshAsync()
    {
        var responding = await Task.Run(() =>
        {
            Process.Refresh();
            return Process.Responding;
        });
        
        if (responding)
            _lastResponse = DateTime.UtcNow;
        
        SourceQueryReader.Refresh();
        Online = SourceQueryReader.Online;
        MaxPlayers = SourceQueryReader.MaxPlayers;
        Players = SourceQueryReader.Players;
        
        
        switch (State)
        {
            case ProcessState.NEW:
                if(!Process.HasExited)
                    State = ProcessState.RUNNING;
                break;
            case ProcessState.RUNNING:
                if (Online) State = ProcessState.ONLINE;
                if (Process.HasExited) State = ProcessState.CRASHED;
                else if (!Process.Responding) State = ProcessState.FROZEN;
                break;
            case ProcessState.ONLINE:
                if (Process.HasExited) State = ProcessState.CRASHED;
                else if (!Process.Responding) State = ProcessState.FROZEN;
                break;
            case ProcessState.FROZEN:
                if (Process.Responding) State = ProcessState.RUNNING;
                else ZombieCheck();
                break;
            case ProcessState.STOPPING:
                if (Process.HasExited) State = ProcessState.STOPPED;
                else SendShutdown();
                break;
            case ProcessState.FAILED:
                State = ProcessState.CRASHED;
                break;
            case ProcessState.RESTARTING:
                if (Process.HasExited) State = ProcessState.NEW;
                else SendShutdown();
                break;
        }
    }

    public void ZombieCheck()
    {
        if (_lastResponse + TimeSpan.FromSeconds(ZombieCheckSeconds) < DateTime.UtcNow)
        {
            State = ProcessState.CRASHED;
            if (KillZombies)
                Process.Kill();
        }
    }

    public Task KillAsync()
    {
        State = ProcessState.STOPPING;
        Process.Kill();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (State is ProcessState.STOPPING or ProcessState.STOPPED or ProcessState.RESTARTING) 
            return Task.CompletedTask;
        State = ProcessState.STOPPING;
        SendShutdown();
        return Task.CompletedTask;
    }

    public Task RestartAsync()
    {
        if (State is ProcessState.STOPPING or ProcessState.STOPPED or ProcessState.RESTARTING) 
            return Task.CompletedTask;
        State = ProcessState.RESTARTING;
        SendShutdown();
        return Task.CompletedTask;
    }

    private void NotifyStateOnline()
    {
        if (_onlineNotified) return;
        _onlineNotified = true;
        foreach (var notifier in Notifiers)
            notifier.Notify(_notifications.GetOnlineNotification(ServerInfos.Title));
    }

    private void NotifyStateCrash()
    {
        if (_crashNotified) return;
        _crashNotified = true;
        foreach (var notifier in Notifiers)
            notifier.Notify(_notifications.GetCrashNotification(ServerInfos.Title));
    }

    private void SendShutdown()
    {
        if ((DateTime.UtcNow - _shutdownCallTime) < TimeSpan.FromMinutes(3)) return;
        _shutdownCallTime = DateTime.UtcNow;
        
        if (RCon is not null && !_hasTriedRConShutdown)
        {
            _hasTriedRConShutdown = true;
            RCon.Send(@"shutdown", CancellationToken.None);
        }
        else if (!_hasTriedHandleShutdown)
        {
            _hasTriedHandleShutdown = true;
            Process.CloseMainWindow();
        }
        else
            Process.Kill();
    }

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

    private void OnStateChanged(ProcessState args)
    {
        switch (args)
        {
            case ProcessState.ONLINE:
                NotifyStateOnline();
                break;
            case ProcessState.CRASHED:
                NotifyStateCrash();
                break;
        }
        
        StateChanged?.Invoke(this, args);
    }
}