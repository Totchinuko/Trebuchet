using System.ComponentModel;
using System.Diagnostics;

namespace TrebuchetLib.Processes;

public interface IConanProcess : IDisposable, INotifyPropertyChanged
{
    int PId { get; }
    
    long MemoryUsage { get; }
    
    TimeSpan CpuTime { get; }
    
    DateTime StartUtc { get; }
    
    ProcessState State { get; }

    Task RefreshAsync();

    Task KillAsync();
}