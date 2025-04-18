using Microsoft.Extensions.Logging;

namespace TrebuchetLib;

public class ConsoleLog
{
    protected ConsoleLog(string body)
    {
        Body = body;
    }

    public string Body { get; }
    public LogLevel LogLevel { get; init; } = LogLevel.Information;
    public DateTime UtcTime { get; init; } = DateTime.UtcNow;
    public ConsoleLogSource Source { get; init; }
        
    public static ConsoleLog CreateError(string body, ConsoleLogSource source)
    {
        return new ConsoleLog(body)
        {
            LogLevel = LogLevel.Error,
            Source = source
        };
    }

    public static ConsoleLog Create(string body, LogLevel level, ConsoleLogSource source)
    {
        return new ConsoleLog(body)
        {
            Source = source,
            LogLevel = level
        };
    }
        
    public static ConsoleLog Create(string body, LogLevel level, DateTime date, ConsoleLogSource source)
    {
        return new ConsoleLog(body)
        {
            Source = source,
            LogLevel = level
        };
    }
}