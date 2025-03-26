using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;

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

    public ConanServerProcess(Process process, ConanServerInfos infos)
    {
        _process = process;
        PId = _process.Id;
        StartUtc = DateTime.UtcNow;
        State = ProcessState.RUNNING;
        _infos = infos;
        _lastResponse = DateTime.UtcNow;
        _sourceQueryReader
            = new SourceQueryReader(new IPEndPoint(IPAddress.Loopback, _infos.QueryPort), 4 * 1000, 5 * 1000);
        
        RCon = new Rcon(new IPEndPoint(IPAddress.Loopback, _infos.RConPort), _infos.RConPassword);
        Console = new MixedConsole(RCon);
    }

    public bool ZombieKill { get; set; }

    public int ZombieCheckSpan { get; set; }

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
    }

    public async Task RefreshAsync()
    {
        _process.Refresh();
        await _sourceQueryReader.Refresh().ConfigureAwait(false);
        Online = _sourceQueryReader.Online;
        MaxPlayers = _sourceQueryReader.MaxPlayers;
        Players = _sourceQueryReader.Players;

        if (State == ProcessState.RUNNING && Online)
            State = ProcessState.ONLINE;

        if (_process.Responding)
            _lastResponse = DateTime.UtcNow;

        if (State is ProcessState.STOPPING or ProcessState.STOPPED or ProcessState.CRASHED)
            return;

        if (_lastResponse + TimeSpan.FromSeconds(ZombieCheckSpan) < DateTime.UtcNow)
        {
            State = ProcessState.CRASHED;
            if (ZombieKill)
                await KillAsync().ConfigureAwait(false);
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