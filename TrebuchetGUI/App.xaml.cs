using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Trebuchet
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly TaskBlocker TaskBlocker = new TaskBlocker();

        public static bool UseSoftwareRendering = true;

        public static string APIKey { get; private set; } = string.Empty;

        public static bool HasCrashed { get; private set; }

        public static bool ImmediateServerCatapult { get; private set; } = false;

        public bool IsShutingDown { get; private set; }

        public static void Crash() => HasCrashed = true;

        public static TrebuchetApp GetApp()
        {
            if (Current.MainWindow is not MainWindow window) throw new Exception("MainWindow is not valid.");
            return window.App;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            IsShutingDown = true;
            Log.Write("Trebuchet is shutting down.", LogSeverity.Info);
            Log.Write("----------------------------------------", LogSeverity.Info);
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Write("Starting Trebuchet", LogSeverity.Info);
            ReadSettings();

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);
            Dispatcher.UnhandledException += new DispatcherUnhandledExceptionEventHandler(OnDispatcherUnhandledException);
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(OnDispatcherUnhandledException);
            TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(OnUnobservedTaskException);

            if (e.Args.Length > 0)
            {
                if (e.Args.Contains("-catapult"))
                    ImmediateServerCatapult = true;
                if (e.Args.Contains("-testlive"))
                {
                    TestliveModal.OpenApp(true);
                    return;
                }
                else if (e.Args.Contains("-live"))
                {
                    TestliveModal.OpenApp(false);
                    return;
                }
            }
            TestliveModal modal = new TestliveModal();
            Current.MainWindow = modal.Window;
            modal.ShowDialog();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Log.Write(e.Exception);
            new ExceptionModal(e.Exception).ShowDialog();
            IsShutingDown = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Current.Dispatcher.Invoke(() =>
            {
                Log.Write((Exception)e.ExceptionObject);
                new ExceptionModal(((Exception)e.ExceptionObject)).ShowDialog();
                IsShutingDown = true;
            });
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Current.Dispatcher.Invoke(() =>
            {
                Log.Write(e.Exception);
                new ExceptionModal(e.Exception).ShowDialog();
                IsShutingDown = true;
            });
        }

        private void ReadSettings()
        {
            var node = JsonSerializer.Deserialize<JsonNode>(GuiExtensions.GetEmbededTextFile("TrebuchetGUI.AppSettings.json"));
            if (node == null) return;

            APIKey = node["apikey"]?.GetValue<string>() ?? string.Empty;
        }
    }
}