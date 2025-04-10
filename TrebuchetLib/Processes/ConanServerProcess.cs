using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using SteamKit2.GC.Dota.Internal;

namespace TrebuchetLib.Processes;

public sealed class ConanServerProcess : IConanServerProcess
{
    private readonly ConanServerInfos _infos;
    private readonly Process _process;
    private readonly SourceQueryReader _sourceQueryReader;
    private DateTime _lastResponse;
    private int _maxPlayers;
    private bool _online;
    private int _players;
    private ProcessState _state;

    public ConanServerProcess(Process process, ConanServerInfos infos, DateTime startTime)
    {
        _process = process;
        PId = _process.Id;
        StartUtc = startTime;
        State = ProcessState.RUNNING;
        _infos = infos;
        _lastResponse = DateTime.UtcNow;
        _sourceQueryReader
            = new SourceQueryReader(new IPEndPoint(IPAddress.Loopback, _infos.QueryPort), 4 * 1000, 5 * 1000);
        _sourceQueryReader.StartQueryThread();
        
        RCon = new Rcon(new IPEndPoint(IPAddress.Loopback, _infos.RConPort), _infos.RConPassword);
        Console = new MixedConsole(RCon);
    }

    public ConanServerProcess(Process process, ConanServerInfos infos) : this(process, infos, DateTime.UtcNow)
    {
    }

    public long MemoryUsage
    {
        get
        {
            if (_process.HasExited)
                return 0;
            return _process.WorkingSet64;
        }
    }

    public TimeSpan CpuTime
    {
        get
        {
            if(_process.HasExited)
                return TimeSpan.Zero;
            return _process.TotalProcessorTime;
        }
    }
    
    public bool KillZombies { get; set; }
    public int ZombieCheckSeconds { get; set; }
    public int PId { get; }
    public DateTime StartUtc { get; }

    public ProcessState State
    {
        get => _state;
        private set => SetField(ref _state, value);
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

    public IConsole Console { get; }
    
    public IRcon RCon { get; }

    public int Instance => _infos.Instance;
    public int Port => _infos.Port;
    public int QueryPort => _infos.QueryPort;
    public string RConPassword => _infos.RConPassword;
    public int RConPort => _infos.RConPort;
    public string Title => _infos.Title;

    public void Dispose()
    {
        _process.Dispose();
        _sourceQueryReader.Dispose();
    }

    public Task RefreshAsync()
    {
        _process.Refresh();
        if (_process.Responding)
            _lastResponse = DateTime.UtcNow;
        
        _sourceQueryReader.Refresh();
        Online = _sourceQueryReader.Online;
        MaxPlayers = _sourceQueryReader.MaxPlayers;
        Players = _sourceQueryReader.Players;
        
        
        switch (State)
        {
            case ProcessState.NEW:
                State = ProcessState.RUNNING;
                break;
            case ProcessState.RUNNING:
                if (Online) State = ProcessState.ONLINE;
                if (_process.HasExited) State = ProcessState.CRASHED;
                else if (!_process.Responding) State = ProcessState.FROZEN;
                break;
            case ProcessState.ONLINE:
                if (_process.HasExited) State = ProcessState.CRASHED;
                else if (!_process.Responding) State = ProcessState.FROZEN;
                break;
            case ProcessState.FROZEN:
                if (_process.Responding) State = ProcessState.RUNNING;
                else ZombieCheck();
                break;
            case ProcessState.STOPPING:
                if (_process.HasExited) State = ProcessState.STOPPED;
                break;
            case ProcessState.FAILED:
                State = ProcessState.CRASHED;
                break;
        }

        return Task.CompletedTask;
    }

    public void ZombieCheck()
    {
        if (_lastResponse + TimeSpan.FromSeconds(ZombieCheckSeconds) < DateTime.UtcNow)
        {
            State = ProcessState.CRASHED;
            if (KillZombies)
                _process.Kill();
        }
    }

    public Task KillAsync()
    {
        State = ProcessState.STOPPING;
        _process.Kill();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        State = ProcessState.STOPPING;
        _process.CloseMainWindow();
        return Task.CompletedTask;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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
}