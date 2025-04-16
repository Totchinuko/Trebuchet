using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TrebuchetLib.Processes;

internal class EmptyConanProcess : IConanProcess
{
    public void Dispose()
    {
    }

    public int PId { get; } = 0;
    public long MemoryUsage { get; } = 0;
    public TimeSpan CpuTime { get; } = TimeSpan.Zero;
    public DateTime StartUtc { get; } = DateTime.MinValue;
    public ProcessState State { get; } = ProcessState.STOPPED;
    
    public event EventHandler<ProcessState>? StateChanged; 
    public event PropertyChangedEventHandler? PropertyChanged;

    public Task RefreshAsync()
    {
        return Task.CompletedTask;
    }

    public Task KillAsync()
    {
        return Task.CompletedTask;
    }
    
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    private void OnStateChanged(ProcessState args)
    {
        StateChanged?.Invoke(this, args);
    }
}

public static class ConanProcess
{
    public static IConanProcess Empty => new EmptyConanProcess();
}

public sealed class ConanClientProcess : IConanProcess
{
    public ConanClientProcess(Process process, DateTime startTime)
    {
        _process = process;
        PId = _process.Id;
        StartUtc = startTime;
        State = ProcessState.RUNNING;
    }
    
    public ConanClientProcess(Process process) : this(process, DateTime.UtcNow)
    {
    }
    
    private readonly Process _process;
    private ProcessState _state;

    public event EventHandler<ProcessState>? StateChanged; 
    public event PropertyChangedEventHandler? PropertyChanged;

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
    
    public int PId { get; }
    public DateTime StartUtc { get; }

    public ProcessState State
    {
        get => _state;
        private set
        {
            if (SetField(ref _state, value))
                OnStateChanged(_state);
        }
    }

    public void Dispose()
    {
        _process.Dispose();
    }

    public Task RefreshAsync()
    {
        _process.Refresh();
        switch (State)
        {
            case ProcessState.RUNNING:
                if (_process.HasExited)
                    State = ProcessState.CRASHED;
                break;
            case ProcessState.STOPPING:
                if (_process.HasExited)
                    State = ProcessState.STOPPED;
                break;
        }

        return Task.CompletedTask;
    }

    public Task KillAsync()
    {
        State = ProcessState.STOPPING;
        _process.Kill();
        return Task.CompletedTask;
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
        StateChanged?.Invoke(this, args);
    }
}