using System;
using System.IO;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using TrebuchetUtils;

namespace Trebuchet.Windows
{
    public partial class MainWindow : WindowAutoPadding
    {
        private bool _shown;
        private TrebuchetApp? _app;

        public MainWindow()
        {
            InitializeComponent();
        }

        public bool WasShown => _shown;

        public void SetApp(TrebuchetApp app)
        {
            _app = app;
            DataContext = app;
        }
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _app?.OnAppClose();
            if(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        }

        //TODO: Test that this event actually work for this purpose
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            if (_shown) return;
            _shown = true;
            OnWindowShown();
        }

        protected virtual void OnWindowShown()
        {
            _app?.OnWindowShow();
        }
    }
}