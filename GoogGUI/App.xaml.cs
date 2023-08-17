using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly TaskBlocker TaskBlocker = new TaskBlocker();

        public static bool UseSoftwareRendering = true;

        public static bool HasCrashed { get; private set; }

        public bool IsShutingDown { get; private set; }

        public static string APIKey { get; private set; } = string.Empty;

        public static void Crash() => HasCrashed = true;

        public GoogApp GetApp()
        {
            if (MainWindow is not MainWindow window) throw new Exception("MainWindow is not valid.");
            return window.App;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            IsShutingDown = true;
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ReadSettings();

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);
            Dispatcher.UnhandledException += new DispatcherUnhandledExceptionEventHandler(OnDispatcherUnhandledException);
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(OnDispatcherUnhandledException);
            TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(OnUnobservedTaskException);

            TestliveModal modal = new TestliveModal();
            Current.MainWindow = modal.Window;
            modal.ShowDialog();
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Current.Dispatcher.Invoke(() =>
            {
                new ExceptionModal(e.Exception).ShowDialog();
                IsShutingDown = true;
            });
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            new ExceptionModal(e.Exception).ShowDialog();
            IsShutingDown = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Current.Dispatcher.Invoke(() =>
            {
                new ExceptionModal(((Exception)e.ExceptionObject)).ShowDialog();
                IsShutingDown = true;
            });
        }

        private void ReadSettings()
        {
            var node = JsonSerializer.Deserialize<JsonNode>(GuiExtensions.GetEmbededTextFile("GoogGUI.AppSettings.json"));
            if (node == null) return;

            APIKey = node["apikey"]?.GetValue<string>() ?? string.Empty;
        }
    }
}