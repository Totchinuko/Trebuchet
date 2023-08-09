using System;
using System.Windows;

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GoogApp? _app;
        private bool _shown;
        private TaskBlocker _taskBlocker = new TaskBlocker();

        public MainWindow(bool testlive)
        {
            _app = new GoogApp(testlive);
            InitializeComponent();
            DataContext = this;
        }

        public GoogApp? App { get => _app; set => _app = value; }

        public TaskBlocker TaskBlocker => _taskBlocker;

        public bool WasShown => _shown;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (_shown) return;
            _shown = true;
            OnWindowShown();
        }

        protected virtual void OnWindowShown()
        {
        }
    }
}