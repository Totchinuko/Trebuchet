using Goog;
using GoogGUI.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GoogApp? _app;
        private bool _shown;

        public MainWindow()
        {
            var modal = new TestliveModal();
            modal.ShowDialog();
            _app = new GoogApp(modal.Testlive);

            InitializeComponent();
            DataContext = this;
        }

        public GoogApp? App { get => _app; set => _app = value; }
        public bool WasShown => _shown;

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