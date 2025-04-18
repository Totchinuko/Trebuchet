using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Cyotek.Collections.Generic;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Trebuchet.Assets;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetUtils;

namespace Trebuchet.ViewModels;

public class MixedConsoleViewModel : ReactiveObject, IScrollController, ITextSource
{
    public const int MAX_LINES = 1000;
    
    public MixedConsoleViewModel(int instance, InternalLogSink trebuchetLog, ILogger logger)
    {
        _instance = instance;
        _logger = logger;
        trebuchetLog.LogReceived += OnTrebuchetLogReceived;
        
        this.WhenAnyValue(x => x.Process)
            .Buffer(2, 1)
            .Select(b => (b[0], b[1]))
            .InvokeCommand(ReactiveCommand.Create<(IConanServerProcess?, IConanServerProcess?)>(OnProcessChanged));

        var canSendCommand = this.WhenAnyValue(x => x.CanSend, x => x.CommandField,
            (c, f) => c && !string.IsNullOrEmpty(f));
            
        SendCommand = ReactiveCommand.CreateFromTask(OnSendCommand, canSendCommand);

        ToggleAutoScroll = ReactiveCommand.Create<Unit>((_) => AutoScroll = !AutoScroll);
        ToggleServerLogs = ReactiveCommand.Create(() =>
        {
            if (!Sources.Contains(ConsoleLogSource.ServerLog)) 
                Sources.Add(ConsoleLogSource.ServerLog);
            else Sources.Remove(ConsoleLogSource.ServerLog);
            DisplayServerLog = Sources.Contains(ConsoleLogSource.ServerLog);
        });
        
        ToggleTrebuchetLogs = ReactiveCommand.Create(() =>
        {
            if (!Sources.Contains(ConsoleLogSource.Trebuchet)) 
                Sources.Add(ConsoleLogSource.Trebuchet);
            else Sources.Remove(ConsoleLogSource.Trebuchet);
            DisplayTrebuchetLog = Sources.Contains(ConsoleLogSource.Trebuchet);
        });
        
        Select = ReactiveCommand.Create(OnConsoleSelected);
        RefreshLabel();
    }

    private bool _displayServerLog;
    private bool _displayTrebuchetLog;
    private readonly CircularBuffer<int> _lineSizes = new(MAX_LINES);
    private readonly StringBuilder _logBuilder = new();
    private readonly int _instance;
    private readonly ILogger _logger;
    private IConanServerProcess? _process;
    private string _text = string.Empty;
    private string _serverLabel = string.Empty;
    private bool _canSend;
    private bool _autoScroll = true;
    private string _commandField = string.Empty;
    private ObservableCollectionExtended<ConsoleLogSource> _sources = [ConsoleLogSource.RCon];

    public event EventHandler<int>? ConsoleSelected; 
    public event EventHandler? ScrollToEnd;
    public event EventHandler? ScrollToHome;
    public event EventHandler<string>? LineAppended;

    public int MaxLines => MAX_LINES;
    
    private ObservableCollectionExtended<ConsoleLogSource> Sources
    {
        get => _sources;
        set => this.RaiseAndSetIfChanged(ref _sources, value);
    }

    public IConanServerProcess? Process
    {
        get => _process;
        set => this.RaiseAndSetIfChanged(ref _process, value);
    }

    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    public bool CanSend
    {
        get => _canSend;
        set => this.RaiseAndSetIfChanged(ref _canSend, value);
    }

    public string ServerLabel
    {
        get => _serverLabel;
        set => this.RaiseAndSetIfChanged(ref _serverLabel, value);
    }

    public bool AutoScroll
    {
        get => _autoScroll;
        set => this.RaiseAndSetIfChanged(ref _autoScroll, value);
    }
    
    public string CommandField
    {
        get => _commandField;
        set => this.RaiseAndSetIfChanged(ref _commandField, value);
    }

    public bool DisplayServerLog
    {
        get => _displayServerLog;
        set => this.RaiseAndSetIfChanged(ref _displayServerLog, value);
    }
    public bool DisplayTrebuchetLog
    {
        get => _displayTrebuchetLog;
        set => this.RaiseAndSetIfChanged(ref _displayTrebuchetLog, value);
    }

    public ReactiveCommand<Unit,Unit> Select { get; }
    public ReactiveCommand<Unit, Unit> SendCommand { get; }
    public ReactiveCommand<Unit,Unit> ToggleServerLogs { get; }
    public ReactiveCommand<Unit,Unit> ToggleTrebuchetLogs { get; }
    public ReactiveCommand<Unit,Unit> ToggleAutoScroll { get; }

    public async Task Send(string input)
    {
        try
        {
            if (Process is not null)
            {
                WriteLine(@"> " + input);
                await Process.Console.Send(input, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, @"Could not Send command");
        }
    }

    public void WriteLine(ConsoleLog log)
    {
        if (!_sources.Contains(log.Source)) return;
        string header = LogLevelHeader(log.UtcTime, log.LogLevel);
        WriteLine(header + log.Body);
    }

    public void WriteLine(LogEventLine log, ConsoleLogSource source)
    {
        if (!_sources.Contains(source)) return;
        string header = LogLevelHeader(log.Date.ToUniversalTime(), log.LogLevel);
        WriteLine(header + log.Output);
    }

    public void WriteLines(IEnumerable<ConsoleLog> logs)
    {
        foreach (var log in logs)
            WriteLine(log);
    }
    
    public void WriteLines(IEnumerable<LogEventLine> logs, ConsoleLogSource source)
    {
        foreach (var log in logs)
            WriteLine(log, source);
    }

    public void WriteLines(params string[] lines)
    {
        foreach (var line in lines)
            WriteLine(line);
    }

    public void WriteLine(string text)
    {
        foreach (var line in SplitLines(text).Select(x => x.Trim() + Environment.NewLine))
            AppendLine(line);
        OnScrollToEnd();
    }

    private string LogLevelHeader(DateTime date, LogLevel level)
    {
        return @$"[{date.ToLocalTime():HH:mm:ss}]{LogLevelToTag(level)} ";
    }

    private string LogLevelToTag(LogLevel level)
    {
        switch (level)  
        {
            case LogLevel.Information:
                return @"[INF]";
            case LogLevel.Debug:
                return @"[DBG]";
            case LogLevel.Critical:
                return @"[CRT]";
            case LogLevel.Error:
                return @"[ERR]";
            case LogLevel.Warning:
                return @"[WRN]";
            default:
                return string.Empty;
        }
    }

    private void OnProcessChanged((IConanServerProcess? old, IConanServerProcess? current) args)
    {
        if (args.old is not null)
        {
            args.old.StateChanged -= OnStateChanged;
            args.old.Console.Received -= OnReceived;
        }

        if (args.current is not null)
        {
            args.current.StateChanged += OnStateChanged;
            args.current.Console.Received += OnReceived;
        }
        RefreshLabel();
    }

    private Task OnReceived(object? sender, ConsoleLogArgs args)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            WriteLines(args.Logs);
        });
        return Task.CompletedTask;
    }
    
    private Task OnTrebuchetLogReceived(object? sender, LogEventArgs args)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            WriteLines(args.Lines, ConsoleLogSource.Trebuchet);
        });
        return Task.CompletedTask;
    }

    private void OnStateChanged(object? sender, ProcessState e)
    {
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (Process is null)
        {
            CanSend = false;
            ServerLabel = $@"{Resources.Unavailable} - {Resources.Instance} {_instance}";
        }
        else
        {
            CanSend = Process is { RConPort: > 0, State: ProcessState.ONLINE };
            ServerLabel = CanSend
                ? $@"{Resources.CatRCon} - {Process.Title} ({Process.Instance}) - {IPAddress.Loopback}:{Process.RConPort}"
                : $@"{Resources.Unavailable} - {Resources.Instance} {_instance}";
        }
    }

    private void AppendLine(string line)
    {
        int remove = 0;
        if (_lineSizes.IsFull)
            remove = _lineSizes.Peek();

        _logBuilder.Remove(0, remove);
        _logBuilder.Append(line);
        _lineSizes.Put(line.Length);
        Text = _logBuilder.ToString();
        LineAppended?.Invoke(this, line);
    }

    [Localizable(false)]
    private IEnumerable<string> SplitLines(string input)
    {
        return input.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    }

    private void OnConsoleSelected()
    {
        ConsoleSelected?.Invoke(this, _instance);
    }

    private void OnScrollToEnd()
    {
        if(AutoScroll)
            ScrollToEnd?.Invoke(this, EventArgs.Empty);
    } 
    
    private void OnScrollToHome()
    {
        if(AutoScroll)
            ScrollToHome?.Invoke(this, EventArgs.Empty);
    }
    
    private async Task OnSendCommand()
    {
        var command = CommandField;
        CommandField = string.Empty;
        await Send(command);
    }

}