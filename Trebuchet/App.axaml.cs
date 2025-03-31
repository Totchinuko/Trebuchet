using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Trebuchet.Modals;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.Utils;
using Trebuchet.ViewModels;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.ViewModels.Panels;
using Trebuchet.Windows;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetLib.YuuIni;
using TrebuchetUtils;
using TrebuchetUtils.Modals;
using TrebuchetUtils.Services.Language;
using Panel = Trebuchet.ViewModels.Panels.Panel;

// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
// Copyright (C) 2025 Totchinuko https://github.com/Totchinuko
// Full license text: LICENSE.txt at the project root

namespace Trebuchet;

public partial class App : Application, IApplication
{
    private ILogger<App>? _logger;
    
    public bool HasCrashed { get; private set; }
    public string AppIconPath => "avares://Trebuchet/Assets/Icons/AppIcon.ico";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void OpenApp(bool testlive)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new Exception("Not supported");

        bool catapult = false;
        if (desktop.Args?.Length > 0)
        {
            if(desktop.Args.Contains("-catapult"))
                catapult = true;
        }
        
        //todo: move to services (And get rid of the tiny return sub messages)
        TinyMessengerHub.Default = new TinyMessengerHub();
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection, testlive, catapult);
        var services = serviceCollection.BuildServiceProvider();
        _logger = services.GetRequiredService<ILogger<App>>();
        
        _logger.LogInformation("Starting Taskmaster");
        _logger.LogInformation($"Selecting {(testlive ? "testlive" : "live")}");

        MainWindow mainWindow = new ();
        desktop.MainWindow = mainWindow;
        mainWindow.SetApp(services.GetRequiredService<TrebuchetApp>());
        mainWindow.Show();
    }
        
    public void Crash() => HasCrashed = true;
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            desktop.ShutdownRequested += OnShutdownRequested;

            if (desktop.Args?.Length > 0)
            {
                if (desktop.Args.Contains("-testlive"))
                {
                    OpenApp(true);
                    return;
                }
                else if (desktop.Args.Contains("-live"))
                {
                    OpenApp(false);
                    return;
                }
            }
            
            TestliveModal modal = new (this);
            desktop.MainWindow = modal.Window;
            modal.Open();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services, bool testlive, bool catapult)
    {
        services.AddSingleton<AppSetup>(
            new AppSetup(Config.LoadConfig(AppConstants.GetConfigPath(testlive)), testlive, catapult));
        services.AddSingleton<UIConfig>(UIConfig.LoadConfig(AppConstants.GetUIConfigPath()));
        
        var logger = new LoggerConfiguration()
#if !DEBUG
            .MinimumLevel.Information()
#endif
            .WriteTo.File(
                AppConstants.GetLoggingPath(),
                retainedFileTimeLimit: TimeSpan.FromDays(7),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddLogging(builder => builder.AddSerilog(logger, true));
        
        services.AddSingleton<AppSettings>(GetAppSettings());
        services.AddSingleton<AppClientFiles>();
        services.AddSingleton<AppServerFiles>();
        services.AddSingleton<AppModlistFiles>();
        services.AddSingleton<AppFiles>();
        services.AddSingleton<IIniGenerator, YuuIniGenerator>();
        services.AddSingleton<IProgressCallback<double>, Progress>();
        services.AddSingleton<Steam>();
        services.AddSingleton<Launcher>();
        services.AddSingleton<TaskBlocker>();
        services.AddSingleton<SteamAPI>();
        services.AddSingleton<ILanguageManager, LanguageManager>();

        services.AddSingleton<SteamWidget>();
        services.AddSingleton<InnerContainer>();
        services.AddTransient<WorkshopSearchViewModel>();
        services.AddTransient<TrebuchetApp>();

        services.AddTransient<Panel, ModlistPanel>();
        services.AddTransient<Panel, ClientProfilePanel>();
        services.AddTransient<Panel, ServerProfilePanel>();
        services.AddTransient<Panel, RconPanel>();
        services.AddTransient<Panel, LogFilterPanel>();
        services.AddTransient<Panel, DashboardPanel>();
        services.AddTransient<Panel, SettingsPanel>();
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        _logger?.LogInformation("Trebuchet off");
        _logger?.LogInformation("----------------------------------------");
    }

    private async void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "DispatcherUnhandledException");
        e.Handled = true;
        await new ExceptionModal(e.Exception).OpenDialogueAsync();
        ShutdownOnError();
    }
    
    private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(() => _logger?.LogError((Exception)e.ExceptionObject, "UnhandledException"));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            new ExceptionModal(((Exception)e.ExceptionObject)).Open();
            ShutdownOnError();
        });
    }

    private async void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(() => _logger?.LogError(e.Exception, "UnobservedTaskException"));
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