using tot_lib;

namespace TrebuchetLib;

public interface ILogReader : IDisposable
{
    public event AsyncEventHandler<LogEventArgs>? LogReceived;
}