using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using tot_lib;

namespace TrebuchetLib;

public partial class LogReader(string logPath) : IDisposable, ILogReader
{
    private long _offset = -1;
    private CancellationTokenSource? _cts;

    public event AsyncEventHandler<LogEventArgs>? LogReceived;
    
    public string LogPath { get; init; } = logPath;

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void Start()
    {
        if (_cts is not null) return;
        _cts = new();
        Task.Run(() => BackgroundThread(_cts.Token), _cts.Token);
    }

    public void Cancel()
    {
        if (_cts is null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private async Task BackgroundThread(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var output = await Read(ct);
                if (!string.IsNullOrEmpty(output))
                    foreach (var log in ParseOutput(output))
                        await OnLogReceived(log);
            }
            catch (Exception ex)
            {
                await OnLogReceived(ex);
                return;
            }
            await Task.Delay(500, ct);
        }
    }

    private async Task<string> Read(CancellationToken ct)
    {
        if (!File.Exists(LogPath)) throw new IOException("File not found" + LogPath);
        await using var fs = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        if (_offset < 0) _offset = fs.Length < 2000 ? 0 : fs.Length - 1;
        fs.Seek(_offset, SeekOrigin.Begin);
        using var sr = new StreamReader(fs);
        var text = await sr.ReadToEndAsync(ct);
        _offset += text.Length;
        return text;
    }

    private IEnumerable<LogEventArgs> ParseOutput(string output)
    {
        var lines = output.Trim().Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var matches = LogRegex().Match(line);
            if(!matches.Success) yield return LogEventArgs.Create(line, DateTime.Now, LogLevel.Information);
            
            var date = ParseDate(matches.Groups[1].Value);
            var source = matches.Groups[3].Value.Trim();
            var level = ParseLogLevel(matches.Groups[4].Value);
            var content = matches.Groups[5].Value;
            yield return LogEventArgs.Create(content, date, level, source);
        }
    }

    private DateTime ParseDate(string data)
    {
        var matches = LogDateRegex().Match(data);
        if (matches.Success)
            return new DateTime(
                int.Parse(matches.Groups[1].Value),
                int.Parse(matches.Groups[2].Value),
                int.Parse(matches.Groups[3].Value),
                int.Parse(matches.Groups[4].Value),
                int.Parse(matches.Groups[5].Value),
                int.Parse(matches.Groups[6].Value)
            );
        return DateTime.Now;
    }

    //Fatal, Error, Warning, Display, Log, Verbose, VeryVerbose, All (=VeryVerbose)
    private LogLevel ParseLogLevel(string data)
    {
        data = data.ToLower().Trim();
        switch (data)
        {
            case "fatal":
                return LogLevel.Critical;
            case "error":
                return LogLevel.Error;
            case "warning":
                return LogLevel.Warning;
            case "display":
            case "log":
                return LogLevel.Information;
            default:
                return LogLevel.Information;
        }
    }

    private async Task OnLogReceived(LogEventArgs log)
    {
        if (LogReceived is not null)
            await LogReceived.Invoke(this, log);
    }
    
    private async Task OnLogReceived(Exception ex)
    {
        if (LogReceived is not null)
            await LogReceived.Invoke(this, LogEventArgs.CreateError(ex));
    }

    //regexr /^\[([0-9\.\-\:]+)\]\[([0-9\s]+)\]([\w\s]+):(?:([\w\s]+):)?(.+)/
    [GeneratedRegex("^\\[([0-9\\.\\-\\:]+)\\]\\[([0-9\\s]+)\\]([\\w\\s]+):(?:([\\w\\s]+):)?(.+)")]
    private static partial Regex LogRegex();
    
    //regexr /([0-9]+)\.([0-9]+)\.([0-9]+)-([0-9]+)\.([0-9]+)\.([0-9]+):([0-9]+)/
    [GeneratedRegex("([0-9]+)\\.([0-9]+)\\.([0-9]+)-([0-9]+)\\.([0-9]+)\\.([0-9]+):([0-9]+)")]
    private static partial Regex LogDateRegex();
}