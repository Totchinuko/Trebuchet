using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace TrebuchetLib;

public partial class LogReader(ILogger<LogReader> logger, string logPath) : IDisposable
{
    private readonly Dictionary<string, object> _loggerContext = new()
    {
        {"TrebSource", ConsoleLogSource.ServerLog},
        {"file", logPath}
    };
    private long _offset = -1;
    private CancellationTokenSource? _cts;
    private DateTime _start = DateTime.MinValue;

    public string LogPath { get; init; } = logPath;

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public LogReader SetContext(string key, object value)
    {
        _loggerContext[key] = value;
        return this;
    }

    public void Start()
    {
        if (_cts is not null) return;
        _cts = new();
        _start = DateTime.UtcNow;
        Task.Run(() => BackgroundThread(_cts.Token), _cts.Token);
    }

    public void StartAtBeginning()
    {
        if (_cts is not null) return;
        _cts = new();
        _start = DateTime.UtcNow;
        _offset = 0;
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
                    ParseAndSend(output);
            }
            catch(IOException){}
            catch (Exception ex)
            {
                using(logger.BeginScope(_loggerContext))
                    logger.LogError(ex, "Could not read logs");
                return;
            }
            await Task.Delay(500, ct);
        }
    }

    private async Task<string> Read(CancellationToken ct)
    {
        if (!File.Exists(LogPath)) throw new IOException("File not found" + LogPath);
        var lastWrite = File.GetLastWriteTimeUtc(LogPath);
        if (lastWrite < _start) return string.Empty;
        
        await using var fs = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        if (_offset < 0) _offset = fs.Length < 2000 ? 0 : fs.Length - 1;
        fs.Seek(_offset, SeekOrigin.Begin);
        using var sr = new StreamReader(fs);
        var text = await sr.ReadToEndAsync(ct);
        _offset += text.Length;
        return text;
    }

    private void ParseAndSend(string output)
    {
        using var scope = logger.BeginScope(_loggerContext);
        var lines = output.Trim().Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var matches = LogRegex().Match(line);
            if (!matches.Success)
            {
                logger.LogInformation(line);
                continue;
            }
            
            //var date = ParseDate(matches.Groups[1].Value);
            var source = matches.Groups[3].Value.Trim();
            var level = ParseLogLevel(matches.Groups[4].Value);
            var content = matches.Groups[5].Value;
            logger.Log(level, "{source}: {content}", source, content);
        }
    }

    // private DateTime ParseDate(string data)
    // {
    //     var matches = LogDateRegex().Match(data);
    //     if (matches.Success)
    //         return new DateTime(
    //             int.Parse(matches.Groups[1].Value),
    //             int.Parse(matches.Groups[2].Value),
    //             int.Parse(matches.Groups[3].Value),
    //             int.Parse(matches.Groups[4].Value),
    //             int.Parse(matches.Groups[5].Value),
    //             int.Parse(matches.Groups[6].Value)
    //         );
    //     return DateTime.Now;
    // }

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

    //regexr /^\[([0-9\.\-\:]+)\]\[([0-9\s]+)\]([\w\s]+):(?:([\w\s]+):)?(.+)/
    [GeneratedRegex("^\\[([0-9\\.\\-\\:]+)\\]\\[([0-9\\s]+)\\]([\\w\\s]+):(?:([\\w\\s]+):)?(.+)")]
    private static partial Regex LogRegex();
    
    // //regexr /([0-9]+)\.([0-9]+)\.([0-9]+)-([0-9]+)\.([0-9]+)\.([0-9]+):([0-9]+)/
    // [GeneratedRegex("([0-9]+)\\.([0-9]+)\\.([0-9]+)-([0-9]+)\\.([0-9]+)\\.([0-9]+):([0-9]+)")]
    // private static partial Regex LogDateRegex();
}