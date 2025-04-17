using Microsoft.Extensions.Logging;
using tot_lib;

namespace TrebuchetLib;

public class LogEventArgs : EventArgs
{
    protected LogEventArgs(string output)
    {
        Output = output;
    }

    public DateTime Date { get; init; } = DateTime.Now;
    public string Output { get; }
    public Exception? Exception { get; init; }
    public LogLevel LogLevel { get; init; }
    public string Category { get; init; } = string.Empty;

    public static LogEventArgs Create(string output, DateTime date, LogLevel level)
    {
        return new LogEventArgs(output)
        {
            Date = date,
            LogLevel = level
        };
    }
    
    public static LogEventArgs Create(string output, DateTime date, LogLevel level, string category)
    {
        return new LogEventArgs(output)
        {
            Date = date,
            LogLevel = level,
            Category = category
        };
    }
    
    public static LogEventArgs CreateError(Exception ex)
    {
        return new LogEventArgs(ex.Message)
        {
            Date = DateTime.Now,
            Exception = ex,
            LogLevel = LogLevel.Error
        };
    }
}

public interface ILogReader
{
    public event AsyncEventHandler<LogEventArgs>? LogReceived;
}