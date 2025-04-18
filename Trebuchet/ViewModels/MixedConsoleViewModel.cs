using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using tot_lib;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetUtils;

namespace Trebuchet.ViewModels;

[Localizable(false)]
public class MixedConsoleViewModel : ReactiveObject, IScrollController
{
    public MixedConsoleViewModel(int instance)
    {
        _instance = instance;
        _block.Classes.Add("console");
        
        this.WhenAnyValue(x => x.Process)
            .Buffer(2, 1)
            .Select(b => (b[0], b[1]))
            .InvokeCommand(ReactiveCommand.Create<(IConanServerProcess?, IConanServerProcess?)>(OnProcessChanged));

        _finalLog = this.WhenAnyValue(x => x.Log)
            .ToProperty(this, x => x.FinalLog);

        Select = ReactiveCommand.Create(OnConsoleSelected);
        RefreshLabel();
    }

    private readonly int _instance;
    private readonly ObservableAsPropertyHelper<string> _finalLog;
    private readonly SelectableTextBlock _block = new ();
    private IConanServerProcess? _process;
    private string _log = string.Empty;
    private string _serverLabel = string.Empty;
    private bool _canSend;
    private bool _autoScroll = true;

    public event EventHandler<int>? ConsoleSelected; 
    public event EventHandler? ScrollToEnd;
    public event EventHandler? ScrollToHome;

    public IConanServerProcess? Process
    {
        get => _process;
        set => this.RaiseAndSetIfChanged(ref _process, value);
    }

    public string Log
    {
        get => _log;
        set => this.RaiseAndSetIfChanged(ref _log, value);
    }

    public bool CanSend
    {
        get => _canSend;
        set => this.RaiseAndSetIfChanged(ref _canSend, value);
    }

    public string FinalLog => _finalLog.Value;

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

    public SelectableTextBlock Block => _block;
    
    public ReactiveCommand<Unit,Unit> Select { get; }

    public async Task Send(string input)
    {
        try
        {
            if (Process is not null)
            {
                WriteLine(@"> " + input, ConsoleColor.Cyan);
                await Process.Console.Send(input, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            WriteLines(ConsoleColor.Red, ex.GetAllExceptions().Split(Environment.NewLine));
        }
    }

    public void WriteLine(ConsoleLog log)
    {
        string header = @$"[{log.UtcTime.ToLocalTime():HH:mm:ss}]{LogLevelToTag(log.LogLevel)} ";
        WriteLine(header + log.Body, LogLevelToColor(log.LogLevel));
    }

    public void WriteLine(string text)
    {
        foreach (var line in LineToInline(SplitLines(text)))
            AppendLine(line);
        OnScrollToEnd();
    }

    public void WriteLines(ConsoleColor color, params string[] lines)
    {
        foreach (var line in lines)
            WriteLine(line, color);
    }

    public void WriteLine(string text, ConsoleColor color)
    {
        foreach (var line in LineToInline(SplitLines(text), color))
            AppendLine(line);
        OnScrollToEnd();
    }

    private string ColorToClass(ConsoleColor color)
    {
        return Enum.GetName(color)?.ToLower() ?? "white";
    }

    private ConsoleColor LogLevelToColor(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Error:
            case LogLevel.Critical:
                return ConsoleColor.Red;
            case LogLevel.Warning:
                return ConsoleColor.Yellow;
            default:
                return ConsoleColor.White;
        }
    }

    private string LogLevelToTag(LogLevel level)
    {
        switch (level)  
        {
            case LogLevel.Information:
                return "[INF]";
            case LogLevel.Debug:
                return "[DBG]";
            case LogLevel.Critical:
                return "[CRT]";
            case LogLevel.Error:
                return "[ERR]";
            case LogLevel.Warning:
                return "[WRN]";
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

    private async Task OnReceived(object? sender, ConsoleLog args)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            WriteLine(args);
        });
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
            ServerLabel = $"Unavailable - Instance {_instance}";
        }
        else
        {
            CanSend = Process is { RConPort: > 0, State: ProcessState.ONLINE };
            ServerLabel = CanSend
                ? $"RCON - {Process.Title} ({Process.Instance}) - {IPAddress.Loopback}:{Process.RConPort}"
                : $"Unavailable - Instance {_instance}";
        }
    }

    private void AppendLine(Inline inline)
    {
        _block.Inlines ??= new InlineCollection();
        if(_block.Inlines.Count == 400)
            _block.Inlines.RemoveAt(0);
        _block.Inlines?.Add(inline);
    }

    private IEnumerable<string> SplitLines(string input)
    {
        return input.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    }

    private IEnumerable<Inline> LineToInline(IEnumerable<string> lines, ConsoleColor color = ConsoleColor.White)
    {
        string cClass = ColorToClass(color);
        return lines.SelectMany<string, Inline>(x =>
        {
            var run = new Run
            {
                Text = x
            };
            run.Classes.Add(cClass);
            return [run, new LineBreak()];
        });
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
}