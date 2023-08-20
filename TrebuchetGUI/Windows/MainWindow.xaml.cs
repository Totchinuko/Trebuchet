using System;
using System.Windows;
using System.Windows.Interop;

namespace Trebuchet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _shown;

        public MainWindow(Config config, UIConfig uiConfig)
        {
            App = new TrebuchetApp(config, uiConfig);
            InitializeComponent();
            DataContext = this;
        }

        public TrebuchetApp App { get; }

        public bool WasShown => _shown;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            App.OnAppClose();
            Application.Current.Shutdown();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (_shown) return;
            _shown = true;
            OnWindowShown();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;

            if (hwndSource != null && Trebuchet.App.UseSoftwareRendering)
                hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;

            base.OnSourceInitialized(e);
        }

        protected virtual void OnWindowShown()
        {
            App.BaseChecks();
        }
    }
}