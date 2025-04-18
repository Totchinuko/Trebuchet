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
using Microsoft.Extensions.Logging;
using ReactiveUI;
using tot_lib;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetUtils;

namespace Trebuchet.ViewModels;

[Localizable(false)]
public class MixedConsoleViewModel : ReactiveObject, IScrollController, ITextSource
{
    public MixedConsoleViewModel(int instance)
    {
        _instance = instance;
        
        this.WhenAnyValue(x => x.Process)
            .Buffer(2, 1)
            .Select(b => (b[0], b[1]))
            .InvokeCommand(ReactiveCommand.Create<(IConanServerProcess?, IConanServerProcess?)>(OnProcessChanged));

        var canSendCommand = this.WhenAnyValue(x => x.CanSend, x => x.CommandField,
            (c, f) => c && !string.IsNullOrEmpty(f));
            
        SendCommand = ReactiveCommand.CreateFromTask(OnSendCommand, canSendCommand);
        
        Select = ReactiveCommand.Create(OnConsoleSelected);
        RefreshLabel();
    }

    private readonly CircularBuffer<int> _lineSizes = new(1000);
    private readonly StringBuilder _logBuilder = new();
    private readonly int _instance;
    private IConanServerProcess? _process;
    private string _text = string.Empty;
    private string _serverLabel = string.Empty;
    private bool _canSend;
    private bool _autoScroll = true;
    private string _commandField = string.Empty;

    public event EventHandler<int>? ConsoleSelected; 
    public event EventHandler? ScrollToEnd;
    public event EventHandler? ScrollToHome;
    public event EventHandler<string>? LineAppended;

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

    public ReactiveCommand<Unit,Unit> Select { get; }
    public ReactiveCommand<Unit, Unit> SendCommand { get; }

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

    public void WriteLine(IEnumerable<ConsoleLog> logs)
    {
        foreach (var log in logs)
            WriteLine(log);
    }

    public void WriteLine(string text)
    {
        WriteLine(text, ConsoleColor.White);
    }

    public void WriteLines(ConsoleColor color, params string[] lines)
    {
        foreach (var line in lines)
            WriteLine(line, color);
    }

    public void WriteLine(string text, ConsoleColor color)
    {
        foreach (var line in LineToFormated(SplitLines(text), color))
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

    private Task OnReceived(object? sender, ConsoleLogArgs args)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            WriteLine(args.Logs);
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

    private IEnumerable<string> SplitLines(string input)
    {
        return input.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    }

    private IEnumerable<string> LineToFormated(IEnumerable<string> lines, ConsoleColor color)
    {
        return lines.Select(x => x + Environment.NewLine);
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