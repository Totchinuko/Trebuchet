using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Display;
using Trebuchet.Assets;
using TrebuchetLib;
using TrebuchetLib.Processes;
using tot_gui_lib;

namespace Trebuchet.ViewModels;

public class MixedConsoleViewModel : ReactiveObject, IScrollController, ITextSource
{
    public const int MAX_CHAR = 500000;
    
    public MixedConsoleViewModel(UIConfig config, int instance, InternalLogSink trebuchetLog, ILogger logger)
    {
        trebuchetLog.LogReceived += OnSinkLogReceived;
        
        Filters.Add(new ConsoleLogFilterViewModel(config, instance, ConsoleLogSource.ServerLog, Resources.ServerLogs, @"mdi-server"));
        Filters.Add(new ConsoleLogFilterViewModel(config, instance, ConsoleLogSource.Trebuchet, Resources.TrebuchetLogs, @"mdi-rocket"));
        
        _instance = instance;
        _logger = logger;
        _instanceEqual = Matching.WithProperty<int>(@"instance", p => p == _instance);
        _hasAnySource = Matching.WithProperty<ConsoleLogSource>(@"TrebSource", _ => true);
        _canBeDisplayed = Matching.WithProperty<ConsoleLogSource>(@"TrebSource", p => Filters
            .Where(x => x.IsDisplayed)
            .Select(x => x.LogSource)
            .Append(ConsoleLogSource.RCon)
            .Contains(p));
        _textWriter = new ConsoleWriter(500, MAX_CHAR);
        _textWriter.TextFlushed += OnTextFlushed;
        
        this.WhenAnyValue(x => x.Process)
            .Buffer(2, 1)
            .Select(b => (b[0], b[1]))
            .InvokeCommand(ReactiveCommand.Create<(IConanServerProcess?, IConanServerProcess?)>(OnProcessChanged));

        var canSendCommand = this.WhenAnyValue(x => x.CanSend, x => x.CommandField,
            (c, f) => c && !string.IsNullOrEmpty(f));
            
        SendCommand = ReactiveCommand.CreateFromTask(OnSendCommand, canSendCommand);

        ToggleAutoScroll = ReactiveCommand.Create<Unit>((_) => AutoScroll = !AutoScroll);

        ClearText = ReactiveCommand.Create(() =>
        {
            _textWriter.Clear();
            TextCleared?.Invoke(this, EventArgs.Empty);
        });
        
        RefreshLabel();
    }

    private Task OnSinkLogReceived(object? sender, IReadOnlyCollection<LogEvent> args)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            WriteLogs(args);
        });
        return Task.CompletedTask;
    }

    private bool _displayServerLog;
    private bool _displayTrebuchetLog;
    private readonly int _instance;
    private readonly ILogger _logger;
    private IConanServerProcess? _process;
    private string _serverLabel = string.Empty;
    private bool _canSend;
    private bool _autoScroll = true;
    private string _commandField = string.Empty;
    private readonly Func<LogEvent, bool> _instanceEqual;
    private readonly Func<LogEvent, bool> _hasAnySource;
    private readonly Func<LogEvent, bool> _canBeDisplayed;
    private readonly ConsoleWriter _textWriter;

    private readonly MessageTemplateTextFormatter _textFormater = new (
        @"[{Timestamp:HH:mm:ss}][{Level:u3}] {TrebSource}: {Message:lj}{NewLine}{Exception}");
    private readonly MessageTemplateTextFormatter _trebFormater = new (
        @"[{Timestamp:HH:mm:ss}][{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

    public event EventHandler? ScrollToEnd;
    public event EventHandler? ScrollToHome;
    public event EventHandler<string>? TextAppended;
    public event EventHandler? TextCleared;

    public int MaxChar => MAX_CHAR;

    public ObservableCollectionExtended<ConsoleLogFilterViewModel> Filters { get; } = [];

    public IConanServerProcess? Process
    {
        get => _process;
        set => this.RaiseAndSetIfChanged(ref _process, value);
    }

    public string Text => _textWriter.Text;

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

    public int Instance => _instance;

    public ReactiveCommand<Unit, Unit> SendCommand { get; }
    public ReactiveCommand<Unit,Unit> ToggleAutoScroll { get; }
    public ReactiveCommand<Unit,Unit> ClearText { get; }

    public async Task Send(string input)
    {
        try
        {
            if (Process?.RCon is not null)
            {
                _logger.LogInformation(@"Send {command}", input);
                await _textWriter.WriteLineAsync(@"> " + input);
                await Process.RCon.Send(input, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, @"Could not Send command");
        }
    }

    public void WriteLogs(IEnumerable<LogEvent> events)
    {
        foreach (var log in events)
        {
            if (_hasAnySource(log))
            {
                if (_instanceEqual(log) && _canBeDisplayed(log)) 
                    _textFormater.Format(log, _textWriter);
            }
            else if(DisplayTrebuchetLog)
                _trebFormater.Format(log, _textWriter);
        }
        _textWriter.Flush();
    }

    private void OnTextFlushed(object? sender, string e)
    {
        if (string.IsNullOrEmpty(e)) return;
        TextAppended?.Invoke(this, e);
        OnScrollToEnd();
    }
    
    private void OnProcessChanged((IConanServerProcess? old, IConanServerProcess? current) args)
    {
        if (args.old is not null)
        {
            args.old.StateChanged -= OnStateChanged;
        }

        if (args.current is not null)
        {
            args.current.StateChanged += OnStateChanged;
        }
        RefreshLabel();
    }
    
    private void OnStateChanged(object? sender, ProcessState e)
    {
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (Process?.RCon is null)
        {
            CanSend = false;
            ServerLabel = $@"{Resources.Unavailable} - {Resources.Instance} {_instance}";
        }
        else
        {
            CanSend = Process is { Infos.RConPort: > 0, State: ProcessState.ONLINE };
            ServerLabel = CanSend
                ? $@"{Resources.CatRCon} - {Process.Infos.Title} ({Process.Infos.Instance}) - {IPAddress.Loopback}:{Process.Infos.RConPort}"
                : $@"{Resources.Unavailable} - {Resources.Instance} {_instance}";
        }
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

    public override string ToString()
    {
        return ServerLabel;
    }
}