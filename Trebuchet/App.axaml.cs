using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Serilog;
using TrebuchetLib;
using TrebuchetUtils;

// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
// Copyright (C) 2023 Totchinuko https://github.com/Totchinuko
// Full license text: LICENSE.txt at the project root

namespace Trebuchet;

public sealed partial class App : Application
{
    public static bool HasCrashed { get; private set; }
    public bool IsShuttingDown { get; private set; }
    public static string ApiKey { get; private set; } = string.Empty;

    public static Dictionary<string, string> AppText { get; set; } = [];

    public static UIConfig Config { get; private set; } = null!;

    public string AppIconPath => "pack://application:,,,/Trebuchet;component/Icons/AppIcon.ico";

    public static string GetAppText(string key, params object[] args)
    {
        return AppText.TryGetValue(key, out var text) ? string.Format(text, args) : $"<INVALID_{key}>";
    }
        
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }


    public static void OpenApp(bool testlive, bool catapult)
    {
        Log.Information($"Selecting {(testlive ? "testlive" : "live")}");
        Config = UIConfig.LoadConfig(UIConfig.GetPath(testlive));

        TrebuchetApp app = new (testlive, catapult);
        MainWindow mainWindow = new (app);
        if(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = mainWindow;
        else throw new Exception("Application not initialized");
        mainWindow.Show();
    }
        
    public static void Crash() => HasCrashed = true;
        
        
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "DispatcherUnhandledException");
        e.Handled = true;
        new ExceptionModal(e.Exception).ShowDialog();
        IsShuttingDown = true;
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Log.Logger = new LoggerConfiguration()
#if !DEBUG
                    .MinimumLevel.Information()
#endif
                .WriteTo.File(
                    Path.Combine(Tools.GetRootPath(), "Logs/app.log"),
                    retainedFileTimeLimit: TimeSpan.FromDays(7),
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();
            Log.Information("Starting Taskmaster");
            
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            desktop.ShutdownRequested += OnShutdownRequested;


            ReadAppText();
            ReadSettings();

            if (desktop.Args?.Length > 0)
            {
                if (desktop.Args.Contains("-testlive"))
                {
                    OpenApp(true, desktop.Args.Contains("-catapult"));
                    return;
                }
                else if (desktop.Args.Contains("-live"))
                {
                    OpenApp(false, desktop.Args.Contains("-catapult"));
                    return;
                }
            }
            TestliveModal modal = new (desktop.Args?.Contains("-catapult") ?? false);
            desktop.MainWindow = modal.Window;
            modal.ShowDialog();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        IsShuttingDown = true;
        Log.Information("Trebuchet off");
        Log.Information("----------------------------------------");
    }

    private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(() => Log.Error((Exception)e.ExceptionObject, "UnhandledException"));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            new ExceptionModal(((Exception)e.ExceptionObject)).ShowDialog();
            IsShuttingDown = true;
        });
    }

    private async void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(() => Log.Error(e.Exception, "UnobservedTaskException"));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            new ExceptionModal(e.Exception).ShowDialog();
            IsShuttingDown = true;
        });
    }

    private void ReadAppText()
    {
        var node = JsonSerializer.Deserialize<JsonNode>(GuiExtensions.GetEmbededTextFile("Trebuchet.Data.AppText.json"));
        if (node == null) return;

        AppText.Clear();
        foreach (var n in node.AsObject())
            AppText.Add(n.Key, n.Value?.GetValue<string>() ?? $"<INVALID_{n.Key}>");
    }

    private static void ReadSettings()
    {
        var node = JsonSerializer.Deserialize<JsonNode>(GuiExtensions.GetEmbededTextFile("Trebuchet.AppSettings.json"));
        if (node == null) return;

        ApiKey = node["apikey"]?.GetValue<string>() ?? string.Empty;
    }
        
    public void Handle(ITinyMessage message, Exception exception)
    {
        new ExceptionModal(exception).ShowDialog();
        IsShuttingDown = true;
        if(Application.Current?.ApplicationLifetime is IControlledApplicationLifetime app)
            app.Shutdown();
        else  throw new Exception("Application not initialized");
    }
}