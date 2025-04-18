using Microsoft.Extensions.Logging;

namespace TrebuchetLib;

public class LogEventLine(string output)
{
    public DateTime Date { get; init; } = DateTime.Now;
    public string Output { get; } = output;
    public Exception? Exception { get; init; }
    public LogLevel LogLevel { get; init; }
    public string Category { get; init; } = string.Empty;
    
    public static LogEventLine Create(string output, DateTime date, LogLevel level)
    {
        return new LogEventLine(output)
        {
            Date = date,
            LogLevel = level
        };
    }
    
    public static LogEventLine Create(string output, DateTime date, LogLevel level, string category)
    {
        return new LogEventLine(output)
        {
            Date = date,
            LogLevel = level,
            Category = category
        };
    }
    
    public static LogEventLine CreateError(Exception ex)
    {
        return new LogEventLine(ex.Message)
        {
            Date = DateTime.Now,
            Exception = ex,
            LogLevel = LogLevel.Error
        };
    }
}