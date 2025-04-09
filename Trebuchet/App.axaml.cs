using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SteamKit2;
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
using TrebuchetUtils.Services.Language;
using Panel = Trebuchet.ViewModels.Panels.Panel;

// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
// Copyright (C) 2025 Totchinuko https://github.com/Totchinuko
// Full license text: LICENSE.txt at the project root

namespace Trebuchet;

public partial class App : Application, IApplication, ISubscriberErrorHandler
{
    private ILogger<App>? _logger;
    
    public bool HasCrashed { get; private set; }
    public IImage? AppIconPath => Resources[@"AppIcon"] as IImage;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void OpenApp(bool testlive)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new Exception(@"Not supported");
        
        bool catapult = false;
        if (desktop.Args?.Length > 0)
        {
            if(desktop.Args.Contains(@"-catapult"))
                catapult = true;
        }
        
        //todo: move to services (And get rid of the tiny return sub messages)
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection, testlive, catapult);
        var services = serviceCollection.BuildServiceProvider();
        _logger = services.GetRequiredService<ILogger<App>>();
        
        _logger.LogInformation(@"Starting Taskmaster");
        _logger.LogInformation(@$"Selecting {(testlive ? @"testlive" : @"live")}");

        MainWindow mainWindow = new ();
        var currentWindow = desktop.MainWindow;
        desktop.MainWindow = mainWindow;
        mainWindow.SetApp(services.GetRequiredService<TrebuchetApp>());
        mainWindow.Show();
        currentWindow?.Close();
    }

    private async void TestError()
    {
        await Task.Delay(500);
        await CrashHandler.Handle(new Exception(@"Test Error"));
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
            
            //CrashHandler.SetReportUri(@"");
            
            if (desktop.Args?.Length > 0)
            {
                if (desktop.Args.Contains(@"-testlive"))
                {
                    OpenApp(true);
                    return;
                }
                else if (desktop.Args.Contains(@"-live"))
                {
                    OpenApp(false);
                    return;
                }
            }
            
            GameBuildViewModel modal = new (this);
            GameBuildWindow window = new GameBuildWindow();
            window.DataContext = modal;
            desktop.MainWindow = window;
            window.Show();
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
        services.AddSingleton<ITinyMessengerHub>(new TinyMessengerHub(this));
        
        services.AddSingleton<AppSettings>(GetAppSettings());
        services.AddSingleton<AppClientFiles>();
        services.AddSingleton<AppServerFiles>();
        services.AddSingleton<AppModlistFiles>();
        services.AddSingleton<AppFiles>();
        services.AddSingleton<OnBoarding>();
        services.AddSingleton<IIniGenerator, YuuIniGenerator>();
        services.AddSingleton<IProgressCallback<double>, Progress>();
        services.AddSingleton<Steam>();
        services.AddSingleton<Launcher>();
        services.AddSingleton<TaskBlocker>();
        services.AddSingleton<SteamAPI>();
        services.AddSingleton<ModFileFactory>();
        services.AddSingleton<ILanguageManager, LanguageManager>();

        services.AddSingleton<SteamWidget>();
        services.AddSingleton<DialogueBox>();
        services.AddSingleton<TrebuchetApp>();
        services.AddTransient<WorkshopSearchViewModel>();

        services.AddSingleton<Panel, ModlistPanel>();
        services.AddSingleton<Panel, ClientProfilePanel>();
        services.AddSingleton<Panel, ServerProfilePanel>();
        #if DEBUG
        services.AddSingleton<Panel, RconPanel>();
        services.AddSingleton<Panel, LogFilterPanel>();
        #endif
        
        services.AddSingleton<Panel, DashboardPanel>();
        services.AddSingleton<Panel, SettingsPanel>();
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        _logger?.LogInformation(@"Trebuchet off");
        _logger?.LogInformation(@"----------------------------------------");
    }

    private async void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, @"DispatcherUnhandledException");
        e.Handled = true;
        await CrashHandler.Handle(e.Exception);
    }
    
    private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger?.LogError((Exception)e.ExceptionObject, @"UnhandledException");
        await CrashHandler.Handle((Exception)e.ExceptionObject);
    }

    private async void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, @"UnobservedTaskException");
        await CrashHandler.Handle(e.Exception);
    }

    private static AppSettings GetAppSettings()
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(
            TrebuchetUtils.Utils.GetEmbeddedTextFile(@"Trebuchet.AppSettings.json"));
        if(settings == null) throw new JsonException(@"AppSettings could not be loaded");
        return settings;
    }
    
    public async void Handle(ITinyMessage message, Exception exception)
    {
        _logger?.LogError(exception, @"UnobservedTaskException");
        await CrashHandler.Handle(exception);
    }
}