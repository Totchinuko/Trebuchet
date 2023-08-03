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
        private Config _config = new Config();
        private Profile? _profile;
        private bool _testlive = false;
        private IGUIPanel? _panel;

        public MainWindow()
        {
            InitializeComponent();
            AppMenu.ContentSelected += OnContentSelected;
            LoadConfiguration();
            DataContext = this;
        }

        protected virtual void LoadConfiguration()
        {
            Config.Load(out _config, _testlive);
        }

        private void OnContentSelected(object? sender, IGUIPanel? e)
        {
            if (_panel != null)
                _panel.Close();
            _panel = e;
            if(_panel != null)
                _panel.Setup(_config, _profile);
            ContentPanelPresenter.Content = _panel;
        }
    }
}