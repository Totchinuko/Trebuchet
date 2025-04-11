using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using tot_lib;

namespace TrebuchetUtils;

internal class CrashHandlerViewModel : ReactiveObject
{

    public CrashHandlerViewModel(Exception ex)
    {
        Title = ex.GetType().Name;
        Message = ex.Message;
        CallStack = ex.GetAllExceptions();

        _foldIcon = this.WhenAnyValue(x => x.FoldedCallstack)
            .Select(x => x ? @"mdi-chevron-right" : @"mdi-chevron-down")
            .ToProperty(this, x => x.FoldIcon);

        _windowHeight = this.WhenAnyValue(x => x.FoldedCallstack)
            .Select(x => x ? 300 : 600)
            .ToProperty(this, x => x.WindowHeight);
        var canSend = this.WhenAnyValue(x => x.ReportSent);

        SendReport = ReactiveCommand.CreateFromTask(SendReportAsync, canSend);
        FoldCallStack = ReactiveCommand.Create<Unit>((_) => FoldedCallstack = !FoldedCallstack);
        FoldedCallstack = true;
        HasReporter = CrashHandler.HasReportUri();
    }
    private bool _foldedCallstack;
    private bool _hasReporter;
    private bool _reportSent;
    private bool _sending;
    private ObservableAsPropertyHelper<string> _foldIcon;
    private ObservableAsPropertyHelper<int> _windowHeight;

    public string Title { get; }
    public string Message { get; }
    public string CallStack { get; }
    public string FoldIcon => _foldIcon.Value;
    public int WindowHeight => _windowHeight.Value;
    
    public ReactiveCommand<Unit, Unit> FoldCallStack { get; }
    public ReactiveCommand<Unit, Unit> SendReport { get; }
    
    public bool FoldedCallstack
    {
        get => _foldedCallstack;
        set => this.RaiseAndSetIfChanged(ref _foldedCallstack, value);
    }
    
    public bool ReportSent
    {
        get => _reportSent;
        set => this.RaiseAndSetIfChanged(ref _reportSent, value);
    }
    
    public bool Sending
    {
        get => _sending;
        set => this.RaiseAndSetIfChanged(ref _sending, value);
    }
    
    public bool HasReporter
    {
        get => _hasReporter;
        set => this.RaiseAndSetIfChanged(ref _hasReporter, value);
    }

    private async Task SendReportAsync()
    {
        ReportSent = true;
        Sending = true;
        var report = new CrashHandlerPayload();
        var result = await CrashHandler.SendReport(report);
        Sending = false;
        ReportSent = result;
    }
}

public static class CrashHandler
{
    private static SemaphoreSlim? _semaphore;
    private static Uri? _reportSendUri;

    public static void SetReportUri(Uri uri) => _reportSendUri = uri;
    public static void SetReportUri(string url) => _reportSendUri = new Uri(url);
    public static bool HasReportUri() => _reportSendUri != null; 

    public static async Task Handle(Exception ex)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Handle(ex);
            });
            return;
        }
        
        if (_semaphore is null)
            _semaphore = new SemaphoreSlim(1, 1);
        await _semaphore.WaitAsync();
        var handler = new CrashHandlerViewModel(ex);
        var window = new CrashHandlerWindow();
        window.DataContext = handler;
        if (Application.Current is not null &&
            Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is not null)
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                await window.ShowDialog(desktop.MainWindow);
            }
            else
            {
                desktop.MainWindow = window;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.Show();
                await WaitForWindow(window);
            }

            _semaphore.Release();
            desktop.Shutdown(1);
            return;
        }

        throw new Exception("Not supported");
    }

    public static async Task<bool> SendReport(CrashHandlerPayload payload)
    {
        if (_reportSendUri == null) return false;
        
        using var httpClient = new HttpClient();
        using var response = await httpClient.PostAsJsonAsync(_reportSendUri, payload);
        return response.IsSuccessStatusCode;
    }

    private static async Task WaitForWindow(Window window)
    {
        while (window.IsActive)
        {
            await Task.Delay(25);
        }
    }
}