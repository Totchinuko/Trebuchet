using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Serilog;
using Trebuchet.Modals;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.Windows;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetLib.YuuIni;
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

    public string AppIconPath => "avares://Trebuchet/Assets/Icons/AppIcon.ico";

    public static string GetAppText(string key, params object[] args)
    {
        return AppText.TryGetValue(key, out var text) ? string.Format(text, args) : $"<INVALID_{key}>";
    }
        
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }


    public async void OpenApp()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new Exception("Not supported");

        bool? testlive = null;
        bool catapult = false;

        if (desktop.Args?.Length > 0)
        {
            if (desktop.Args.Contains("-testlive"))
                testlive = true;
            else if (desktop.Args.Contains("-live"))
                testlive = false;
            if(desktop.Args.Contains("-catapult"))
                catapult = true;
        }
        
        MainWindow mainWindow = new ();
        desktop.MainWindow = mainWindow;

        if (testlive is null)
        {
            TestliveModal modal = new ();
            await modal.OpenDialogueAsync();
            testlive = modal.Result;
        }
        
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
        
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection, (bool)testlive, catapult);
        
        var services = serviceCollection.BuildServiceProvider();
        
        Log.Information($"Selecting {((bool)testlive ? "testlive" : "live")}");

        mainWindow.SetApp(services.GetRequiredService<TrebuchetApp>());
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

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            desktop.ShutdownRequested += OnShutdownRequested;

            ReadAppText();
            
            OpenApp();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services, bool testlive, bool catapult)
    {
        services.AddSingleton<AppSettings>(GetAppSettings());
        services.AddSingleton<AppClientFiles>();
        services.AddSingleton<AppServerFiles>();
        services.AddSingleton<AppModlistFiles>();
        services.AddSingleton<AppFiles>();
        services.AddSingleton<AppSetup>(
            new AppSetup(Config.LoadConfig(Utils.Utils.GetConfigPath()), testlive, catapult));
        services.AddSingleton<IIniGenerator, YuuIniGenerator>();
        services.AddSingleton<IProgress<double>, Progress>();
        services.AddSingleton<Steam>();
        services.AddSingleton<Launcher>();
        services.AddSingleton<UIConfig>(UIConfig.LoadConfig(UIConfig.GetUIConfigPath()));
        services.AddSingleton<TaskBlocker>();
        services.AddSingleton<SteamAPI>();

        services.AddTransient<TrebuchetApp>();

        var panels = typeof(Panel).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Panel))).ToList();
        foreach (var panel in panels)
            services.AddTransient(panel);
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