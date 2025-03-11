using System;
using System.IO;
using Avalonia.Controls;
using Avalonia;

namespace Trebuchet
{
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
            App.OnWindowShow();
        }
    }
}