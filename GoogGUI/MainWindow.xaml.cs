﻿using Goog;
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
            InitializeComponent();
        }

        public GoogApp? App { get => _app; set => _app = value; }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (_shown) return;
            _shown = true;
            OnWindowShown();
        }

        protected virtual void OnWindowShown()
        {
            var modal = new TestliveModal();
            modal.ShowDialog();
            _app = new GoogApp(modal.Testlive);
            DataContext = this;
        }
    }
}