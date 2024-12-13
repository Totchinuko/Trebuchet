using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Serilog;
using TrebuchetGUILib;

/// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
/// Copyright (C) 2023 Totchinuko https://github.com/Totchinuko
/// Full license text: LICENSE.txt at the project root

namespace Trebuchet
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : TrebuchetBaseApp
    {
        public static string APIKey { get; private set; } = string.Empty;

        public static Dictionary<string, string> AppText { get; set; } = new Dictionary<string, string>();

        public static UIConfig Config { get; private set; } = default!;

        public override string AppIconPath => "pack://application:,,,/Trebuchet;component/Icons/AppIcon.ico";

        public static string GetAppText(string key, params object[] args)
        {
            if (AppText.TryGetValue(key, out var text)) return string.Format(text, args);
            return $"<INVALID_{key}>";
        }

        public static void OpenApp(bool testlive, bool catapult)
        {
            Log.Information($"Selecting {(testlive ? "testlive" : "live")}");
            Config = UIConfig.LoadConfig(UIConfig.GetPath(testlive));

            TrebuchetApp app = new TrebuchetApp(testlive, catapult);
            MainWindow mainWindow = new MainWindow(app);
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "DispatcherUnhandledException");
            base.OnDispatcherUnhandledException(sender, e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Trebuchet off");
            Log.Information("----------------------------------------");
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
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

            base.OnStartup(e);

            ReadAppText();
            ReadSettings();

            if (e.Args.Length > 0)
            {
                if (e.Args.Contains("-testlive"))
                {
                    OpenApp(true, e.Args.Contains("-catapult"));
                    return;
                }
                else if (e.Args.Contains("-live"))
                {
                    OpenApp(false, e.Args.Contains("-catapult"));
                    return;
                }
            }
            TestliveModal modal = new TestliveModal(e.Args.Contains("-catapult"));
            Current.MainWindow = modal.Window;
            modal.ShowDialog();
        }

        protected override async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            await Current.Dispatcher.InvokeAsync(() => Log.Error((Exception)e.ExceptionObject, "UnhandledException"));
            base.OnUnhandledException(sender, e);
        }

        protected override async void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            await Current.Dispatcher.InvokeAsync(() => Log.Error(e.Exception, "UnobservedTaskException"));
            base.OnUnobservedTaskException(sender, e);
        }

        private void ReadAppText()
        {
            var node = JsonSerializer.Deserialize<JsonNode>(GuiExtensions.GetEmbededTextFile("Trebuchet.Data.AppText.json"));
            if (node == null) return;

            AppText.Clear();
            foreach (var n in node.AsObject())
                AppText.Add(n.Key, n.Value?.GetValue<string>() ?? $"<INVALID_{n.Key}>");
        }

        private void ReadSettings()
        {
            var node = JsonSerializer.Deserialize<JsonNode>(GuiExtensions.GetEmbededTextFile("Trebuchet.AppSettings.json"));
            if (node == null) return;

            APIKey = node["apikey"]?.GetValue<string>() ?? string.Empty;
        }
    }
}