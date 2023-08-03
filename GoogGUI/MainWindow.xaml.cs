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
        private GoogApp _app;

        public MainWindow()
        {
            InitializeComponent();
            _app = new GoogApp();
            DataContext = this;
        }

        public GoogApp App { get => _app; set => _app = value; }
    }
}