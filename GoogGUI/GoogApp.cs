using Goog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GoogGUI
{
    public class GoogApp : INotifyPropertyChanged
    {
        private Config _config;
        private IPanel? _panel;
        private List<IPanel> _topTabs;
        private List<IPanel> _bottomTabs;

        public GoogApp(Config config)
        {
            _config = config;
            _topTabs = new List<IPanel>
            {
                new ModlistHandler(_config),
                new ClientSettings(_config),
            };

            _bottomTabs = new List<IPanel>
            {
                new Settings(_config),
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public IPanel? Panel 
        { 
            get => _panel; 
            set
            {
                if (_panel != null)
                    _panel.Active = false;
                _panel = value;
                if (_panel != null)
                    _panel.Active = true;
                OnPropertyChanged("Panel");
            }
        }
        public List<IPanel> TopTabs { get => _topTabs; }
        public List<IPanel> BottomTabs { get => _bottomTabs; }

        public void BaseChecks()
        {
            if (!string.IsNullOrEmpty(_config.InstallPath) && !Tools.CanWriteHere(_config.InstallPath))
                new ErrorModal("Install Folder Error", "Cannot access the install folder", false).ShowDialog();

            if (string.IsNullOrEmpty(_config.InstallPath))
                new MessageModal("Install Folder", "In order to use Goog, please configure a folder to install your mods and profiles").ShowDialog();

            if (!_config.IsInstallPathValid)
                Panel = _bottomTabs[0];
        }

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}