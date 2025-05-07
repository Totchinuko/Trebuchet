using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using AvaloniaEdit.Utils;
using Cyotek.Collections.Generic;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.File;
using tot_lib;
using TrebuchetLib;

namespace Trebuchet;

public class InternalLogSink : IBatchedLogEventSink, IDisposable
{
    private readonly CircularBuffer<LogEvent> _eventBuffer = new(100);
    private bool _disposed;

    private readonly Func<LogEvent, bool> _bufferFilter
        = Matching.WithProperty<ConsoleLogSource>(@"TrebSource", _ => true);
    
    public event AsyncEventHandler<IReadOnlyCollection<LogEvent>>? LogReceived;
    
    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        if (_disposed) return;
        _eventBuffer.AddRange(batch.Where((l) => !_bufferFilter(l)));
        if (LogReceived is null) return;
        if(batch.Count > 0)
            await LogReceived.Invoke(this, batch);
    }

    public IReadOnlyCollection<LogEvent> GetLastLogs()
    {
        return _eventBuffer.ToList();
    }

    public void Dispose()
    {
        _disposed = true;
        LogReceived = null;
        _eventBuffer.Clear();
    }
}