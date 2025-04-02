using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TrebuchetLib.Processes;

public sealed class ConanClientProcess : IConanProcess
{
    private readonly Process _process;

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

    public ProcessState State { get; private set; }

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
}