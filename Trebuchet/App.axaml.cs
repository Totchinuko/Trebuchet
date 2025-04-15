using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using tot_lib;
using Trebuchet.Services;
using Trebuchet.Services.Language;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.Utils;
using Trebuchet.ViewModels;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.ViewModels.Panels;
using Trebuchet.Windows;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetLib.Services.Importer;
using TrebuchetLib.YuuIni;
using TrebuchetUtils;

// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
// Copyright (C) 2025 Totchinuko https://github.com/Totchinuko
// Full license text: LICENSE.txt at the project root

namespace Trebuchet;

public partial class App : Application, IApplication
{
    private ILogger<App>? _logger;
    private UIConfig? _uiConfig;
    private LanguageManager? _langManager;
    public bool HasCrashed { get; private set; }
    public IImage? AppIconPath => Resources[@"AppIcon"] as IImage;

    public override void Initialize()
    {
        _uiConfig = UIConfig.LoadConfig(AppConstants.GetUIConfigPath());
        var languageConfiguration = new LanguagesConfiguration(AppConstants.UICultureList);
        _langManager = new LanguageManager(languageConfiguration);
        _langManager.SetLanguage(_uiConfig.UICulture);
        
        AvaloniaXamlLoader.Load(this);
    }

    public void OpenApp(bool testlive)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new Exception(@"Not supported");
        
        bool catapult = false;
        bool experiment = false;
        if (desktop.Args?.Length > 0)
        {
            if(desktop.Args.Contains(Constants.argCatapult))
                catapult = true;
            if (desktop.Args.Contains(Constants.argExperiment))
                experiment = true;
        }
        
        //todo: move to services (And get rid of the tiny return sub messages)
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection, testlive, catapult, experiment);
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
            
            Utils.Utils.ApplyPlateformTheme((PlateformTheme)_uiConfig!.PlateformTheme);
            
            if (desktop.Args?.Length > 0)
            {
                if (desktop.Args.Contains(Constants.argTestLive))
                {
                    OpenApp(true);
                    return;
                }
                else if (desktop.Args.Contains(Constants.argLive))
                {
                    OpenApp(false);
                    return;
                }
            }
            
            GameBuildViewModel modal = new (this);
            GameBuildWindow window = new ()
            {
                DataContext = modal
            };
            desktop.MainWindow = window;
            window.Show();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services, bool testlive, bool catapult, bool experiment)
    {
        services.AddSingleton(
            new AppSetup(Config.LoadConfig(Constants.GetConfigPath(testlive)), testlive, catapult, experiment));
        services.AddSingleton(_uiConfig!);
        services.AddSingleton<ILanguageManager>(_langManager!);
        services.AddSingleton<IUpdater>(
            new GithubUpdater(
                AppConstants.GithubOwnerUpdate,
                AppConstants.GithubRepoUpdate,
                AppConstants.GetUpdateContentType()));
        
        var logger = new LoggerConfiguration()
#if !DEBUG
            .MinimumLevel.Information()
#endif
            .WriteTo.File(
                Path.Combine(Constants.GetLoggingDirectory().FullName, @"app.log"),
                retainedFileTimeLimit: TimeSpan.FromDays(7),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddLogging(builder => builder.AddSerilog(logger, true));
        
        services.AddSingleton<AppClientFiles>();
        services.AddSingleton<AppServerFiles>();
        services.AddSingleton<AppModlistFiles>();
        services.AddSingleton<AppFiles>();
        services.AddSingleton<ModlistImporter>();
        services.AddSingleton<OnBoarding>();
        services.AddSingleton<IIniGenerator, YuuIniGenerator>();
        services.AddSingleton<IProgressCallback<double>, Progress>();
        services.AddSingleton<Steam>();
        services.AddSingleton<Launcher>();
        services.AddSingleton<TaskBlocker>();
        services.AddSingleton<SteamApi>();
        services.AddSingleton<ModFileFactory>();

        services.AddSingleton<SteamWidget>();
        services.AddSingleton<DialogueBox>();
        services.AddSingleton<TrebuchetApp>();
        services.AddTransient<WorkshopSearchViewModel>();

        services.AddSingleton<IPanel, ModlistPanel>();
        services.AddSingleton<IPanel, ClientProfilePanel>();
        services.AddSingleton<IPanel, ServerProfilePanel>();
       
        services.AddSingleton<IPanel, DashboardPanel>();
        services.AddSingleton<IPanel, ToolboxPanel>();
        services.AddSingleton<IPanel, SettingsPanel>();
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        _logger?.LogInformation(@"Trebuchet off");
        _logger?.LogInformation(@"----------------------------------------");
    }

    private async void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            _logger?.LogError(e.Exception, @"DispatcherUnhandledException");
            e.Handled = true;
            await CrashHandler.Handle(e.Exception);
        }
        catch(Exception ex)
        {
            _logger?.LogCritical(ex, @"OnDispatcherUnhandledException");
        }
    }
    
    private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            _logger?.LogError((Exception)e.ExceptionObject, @"UnhandledException");
            await CrashHandler.Handle((Exception)e.ExceptionObject);
        }
        catch(Exception ex)
        {
            _logger?.LogCritical(ex, @"OnUnhandledException");
        }
    }

    private async void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            _logger?.LogError(e.Exception, @"UnobservedTaskException");
            await CrashHandler.Handle(e.Exception);
        }
        catch(Exception ex)
        {
            _logger?.LogCritical(ex, @"OnUnobservedTaskException");
        }
    }
}