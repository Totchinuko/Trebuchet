using Goog;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public bool IsShutingDown { get; private set; }
        public static bool UseSoftwareRendering = true;

        public static readonly TaskBlocker TaskBlocker = new TaskBlocker();

        protected override void OnExit(ExitEventArgs e)
        {
            IsShutingDown = true;
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);
            System.Windows.Forms.Application.ThreadException += new ThreadExceptionEventHandler(OnApplicationThreadException);
            Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(OnDispatcherUnhandledException);

            TestliveModal modal = new TestliveModal();
            Current.MainWindow = modal.Window;
            modal.ShowDialog();
        }

        private void OnApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            new ExceptionModal(e.Exception).ShowDialog();
            IsShutingDown = true;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            new ExceptionModal(e.Exception).ShowDialog();
            IsShutingDown = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            new ExceptionModal(((Exception)e.ExceptionObject)).ShowDialog();
            IsShutingDown = true;
        }
    }
}