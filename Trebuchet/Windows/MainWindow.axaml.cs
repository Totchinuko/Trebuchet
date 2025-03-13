using System;
using System.IO;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Trebuchet
{
    public partial class MainWindow : Window
    {
        private bool _shown;

        public MainWindow()
        {
            InitializeComponent();
        }

        public TrebuchetApp? App { get; private set; }

        public bool WasShown => _shown;

        public void SetApp(TrebuchetApp app)
        {
            App = app;
            DataContext = this;
        }
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            App?.OnAppClose();
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
            App?.OnWindowShow();
        }
    }
}