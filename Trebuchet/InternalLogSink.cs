using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using tot_lib;
using TrebuchetLib;

namespace Trebuchet;

public class InternalLogSink : IBatchedLogEventSink, ILogReader
{
    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        if (LogReceived is null) return;
        LogEventArgs args = new LogEventArgs();
        foreach (var logEvent in batch)
        {
            if (logEvent.Exception is not null)
                args.Append(LogEventLine.CreateError(logEvent.Exception));
            else
                args.Append(LogEventLine.Create(
                    logEvent.MessageTemplate.Text, 
                    logEvent.Timestamp.LocalDateTime, 
                    ToLogLevel(logEvent.Level)));
        }

        if(args.Lines.Count > 0)
            await LogReceived.Invoke(this, args);
    }

    public void Dispose()
    {
    }

    private LogLevel ToLogLevel(LogEventLevel seriLevel)
    {
        switch (seriLevel)
        {
            case LogEventLevel.Debug:
                return LogLevel.Debug;
            case LogEventLevel.Error:
                return LogLevel.Error;
            case LogEventLevel.Fatal:
                return LogLevel.Critical;
            case LogEventLevel.Information:
                return LogLevel.Information;
            case LogEventLevel.Verbose:
                return LogLevel.Debug;
            case LogEventLevel.Warning:
                return LogLevel.Warning;
            default:
                return LogLevel.None;
        }
    }

    public event AsyncEventHandler<LogEventArgs>? LogReceived;
}