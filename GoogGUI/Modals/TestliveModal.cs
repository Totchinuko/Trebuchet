﻿using Goog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    public class TestliveModal : BaseModal
    {
        private bool _madeASelection;
        private bool _testlive = false;

        public TestliveModal() : base()
        {
            LiveCommand = new SimpleCommand(OnLiveClicked);
            TestLiveCommand = new SimpleCommand(OnTestLiveClicked);
        }

        public ICommand LiveCommand { get; private set; }
        public override int ModalHeight => 400;

        public override string ModalTitle => "Game Build";

        public override bool CloseDisabled => false;

        public override int ModalWidth => 400;
        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["TestliveSelection"];
        public bool Testlive { get => _testlive; private set => _testlive = value; }
        public ICommand TestLiveCommand { get; private set; }

        public override void OnWindowClose()
        {
            if (!_madeASelection)
                Application.Current.Shutdown();
        }

        private void OnLiveClicked(object? obj)
        {
            _madeASelection = true;
            OpenApp();
        }

        private void OnTestLiveClicked(object? obj)
        {
            _madeASelection = true;
            _testlive = true;
            OpenApp();
        }

        private void OpenApp()
        {
            Config config = Config.LoadFile(Config.GetPath(Testlive));
            App.UseSoftwareRendering = !config.UseHardwareAcceleration;

            MainWindow mainWindow = new MainWindow(config);
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            _window.Close();
        }
    }
}