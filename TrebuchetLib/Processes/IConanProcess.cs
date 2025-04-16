using System.ComponentModel;
using System.Diagnostics;
using tot_lib;

namespace TrebuchetLib.Processes;

public interface IConanProcess : IDisposable, INotifyPropertyChanged
{
    int PId { get; }
    
    long MemoryUsage { get; }
    
    TimeSpan CpuTime { get; }
    
    DateTime StartUtc { get; }
    
    ProcessState State { get; }

    event EventHandler<ProcessState>? StateChanged;

    Task RefreshAsync();

    Task KillAsync();
}