using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shapes;

namespace Trebuchet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _shown;

        public MainWindow(TrebuchetApp app)
        {
            App = app;
            InitializeComponent();
            DataContext = this;
        }

        public TrebuchetApp App { get; }

        public bool WasShown => _shown;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            App.OnAppClose();
            Application.Current.Shutdown(0);
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
            App.OnWindowShow();
        }
    }
}