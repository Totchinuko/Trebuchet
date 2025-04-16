using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using tot_lib;
using TrebuchetLib;
using TrebuchetLib.Processes;
using TrebuchetUtils;

namespace Trebuchet.ViewModels;

[Localizable(false)]
public class MixedConsoleViewModel : ReactiveObject, IScrollController
{
    public const string NEWLINE = "<br />";
    public const string SPANIN = "<span class\".{0}\">";
    public const string SPANOUT = "</span>";
    public const string STARTLOG = "<p>";
    public const string ENDLOG = "</p>";
    public const string TABSPACE = "&nbsp;&nbsp;&nbsp;&nbsp;";
    
    public MixedConsoleViewModel(int instance)
    {
        _instance = instance;
        
        this.WhenAnyValue(x => x.Process)
            .Buffer(2, 1)
            .Select(b => (b[0], b[1]))
            .InvokeCommand(ReactiveCommand.Create<(IConanServerProcess?, IConanServerProcess?)>(OnProcessChanged));

        _header = TrebuchetUtils.Utils.GetMarkdownHtmlHeader();

        _finalLog = this.WhenAnyValue(x => x.Log)
            .Select(x => _header + STARTLOG + x + ENDLOG)
            .ToProperty(this, x => x.FinalLog);
        
        

        Select = ReactiveCommand.Create(OnConsoleSelected);
        RefreshLabel();
    }

    private readonly int _instance;
    private IConanServerProcess? _process;
    private string _log = string.Empty;
    private string _serverLabel = string.Empty;
    private ObservableAsPropertyHelper<string> _finalLog;
    private string _header;
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
    
    public ReactiveCommand<Unit,Unit> Select { get; }

    public void Send(string input)
    {
        try
        {
            Process?.Console.SendCommand(input, CancellationToken.None);
        }
        catch (Exception ex)
        {
            WriteLines(ConsoleColor.Red, ex.GetAllExceptions().Split(Environment.NewLine));
        }
    }

    public void WriteLine(ConsoleLog log)
    {
        string header = log.IsReceived ? @$"[{log.UtcTime.ToLocalTime():HH:mm:ss}] " : @"> ";
        if(log.IsError)
            WriteLine(header + log.Body, ConsoleColor.Red);
        else
            WriteLine(header + log.Body);
    }

    public void WriteLine(string line)
    {
        Log += ConsoleLineToHtml(line) + NEWLINE;
        OnScrollToEnd();
    }

    public void WriteLines(ConsoleColor color, params string[] lines)
    {
        foreach (var line in lines)
            WriteLine(line, color);
    }

    public void WriteLine(string line, ConsoleColor color)
    {
        Log += string.Format(SPANIN, Enum.GetName(color)) + ConsoleLineToHtml(line) + SPANOUT + NEWLINE;
        OnScrollToEnd();
    }

    private void OnProcessChanged((IConanServerProcess? old, IConanServerProcess? current) args)
    {
        if (args.old is not null)
        {
            args.old.StateChanged -= OnStateChanged;
            args.old.Console.LogReceived -= OnLogReceived;
        }

        if (args.current is not null)
        {
            args.current.StateChanged += OnStateChanged;
            args.current.Console.LogReceived += OnLogReceived;
        }
        RefreshLabel();
    }

    private Task OnLogReceived(object? sender, ConsoleLogEventArgs args)
    {
        WriteLine(args.ConsoleLog);
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

    private string ConsoleLineToHtml(string line)
    {
        line = WebUtility.HtmlEncode(line).Trim();
        line = line.Replace("\t", TABSPACE);
        var lines = line.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        line = string.Join(NEWLINE, lines);
        return line;
    }
}