using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Serilog;
using Trebuchet.Modals;
using Trebuchet.Windows;
using TrebuchetLib;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
// Copyright (C) 2023 Totchinuko https://github.com/Totchinuko
// Full license text: LICENSE.txt at the project root

namespace Trebuchet;

public partial class App : Application, IApplication
{
    public bool HasCrashed { get; private set; }

    public static Dictionary<string, string> AppText { get; set; } = [];

    public static UIConfig Config { get; private set; } = null!;
    
    public string AppIconPath => "avares://Trebuchet/Assets/Icons/AppIcon.ico";

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

        MainWindow mainWindow = new ();
        mainWindow.SetApp(new (testlive, catapult));
        if(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = mainWindow;
        else throw new Exception("Application not initialized");
        mainWindow.Show();
    }
        
    public void Crash() => HasCrashed = true;
        
        
    private async void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "DispatcherUnhandledException");
        e.Handled = true;
        await new ExceptionModal(e.Exception).OpenDialogueAsync();
        ShutdownOnError();
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        
        var services = serviceCollection.BuildServiceProvider();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // todo di:this will need to be service
            TinyMessengerHub.Default = new TinyMessengerHub();
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
            modal.Open();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<AppSettings>(GetAppSettings());
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        Log.Information("Trebuchet off");
        Log.Information("----------------------------------------");
    }

    private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(() => Log.Error((Exception)e.ExceptionObject, "UnhandledException"));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            new ExceptionModal(((Exception)e.ExceptionObject)).Open();
            ShutdownOnError();
        });
    }

    private async void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(() => Log.Error(e.Exception, "UnobservedTaskException"));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            DisplayExceptionAndExit(e.Exception);
        });
    }

    private async void DisplayExceptionAndExit(Exception ex)
    {
        await new ExceptionModal(ex).OpenDialogueAsync();
        ShutdownOnError();
    }

    private void ReadAppText()
    {
        var node = JsonSerializer.Deserialize<JsonNode>(TrebuchetUtils.Utils.GetEmbeddedTextFile("Trebuchet.Data.AppText.json"));
        if (node == null) return;

        AppText.Clear();
        foreach (var n in node.AsObject())
            AppText.Add(n.Key, n.Value?.GetValue<string>() ?? $"<INVALID_{n.Key}>");
    }

    private static AppSettings GetAppSettings()
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(
            TrebuchetUtils.Utils.GetEmbeddedTextFile("Trebuchet.AppSettings.json"));
        if(settings == null) throw new JsonException("AppSettings could not be loaded");
        return settings;
    }
        
    public void Handle(ITinyMessage message, Exception exception)
    {
        DisplayExceptionAndExit(exception);
    }

    private void ShutdownOnError()
    {
        if(Current?.ApplicationLifetime is  IControlledApplicationLifetime app)
            app.Shutdown();
    }
}