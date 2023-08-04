using Goog;
using GoogLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    public class GoogApp : INotifyPropertyChanged
    {
        private Config _config;
        private string _currentProfile = string.Empty;
        private object? _panel;
        private Profile? _profile;
        private List<string> _profiles = new List<string>();
        private bool _testlive;

        public GoogApp(bool testlive)
        {
            SettingsCommand = new SimpleCommand(DisplaySettings);
            ModlistCommand = new SimpleCommand(ModlistDisplay);
            _testlive = testlive;
            Config.Load(out _config, _testlive);

            if (!string.IsNullOrEmpty(_config.InstallPath) && !Tools.CanWriteHere(_config.InstallPath))
                new ErrorModal("Install Folder Error", "Cannot access the install folder", false).ShowDialog();

            if (string.IsNullOrEmpty(_config.InstallPath))
                new ErrorModal("Install Folder", "In order to use Goog, please configure a folder to install your mods and profiles", false).ShowDialog();

            if (!_config.IsInstallPathValid)
                DisplaySettings(this);
            RefreshConfig();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanUseGame => IsProfileLoaded && !string.IsNullOrEmpty(_config.ClientPath) && _config.ClientBin.Exists;

        public bool CanUseServer => IsProfileLoaded && _config.ServerBin.Exists;

        public string CurrentProfile { get => _currentProfile; set => _currentProfile = value; }

        public bool IsProfileLoaded => _profile != null;

        public ICommand ModlistCommand { get; private set; }

        public object? Panel { get => _panel; set => _panel = value; }

        public List<string> Profiles { get => _profiles; set => _profiles = value; }

        public ICommand SettingsCommand { get; private set; }

        public void DisplaySettings(object? sender)
        {
            Settings setting = new Settings(_config);
            setting.ConfigChanged += OnConfigChanged;
            _panel = setting;
            OnPropertyChanged("Panel");
        }

        public void LoadProfile()
        {
            _profile = null;
            if (!_config.ProfileExists(_currentProfile))
            {
                if (!_config.TryGetFirstProfile(out _currentProfile))
                    return;
                _config.CurrentProfile = _currentProfile;
                _config.SaveConfig();
                OnPropertyChanged("CurrentProfile");
            }

            if (!_config.ProfileExists(_currentProfile))
                return;

            Profile.Load(_testlive, _currentProfile, _config, out _profile);
            OnPropertyChanged("IsProfileLoaded");
        }

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void ModlistDisplay(object? obj)
        {
        }

        private void OnConfigChanged(object? sender, EventArgs e)
        {
            RefreshConfig();
            OnPropertyChanged("CanUseGame");
            OnPropertyChanged("CanUseServer");
        }

        private void RefreshConfig()
        {
            _profiles = _config.GetAllProfiles();
            _currentProfile = _config.CurrentProfile;
            LoadProfile();
            OnPropertyChanged("Profiles");
            OnPropertyChanged("CurrentProfile");
            OnPropertyChanged("IsProfileLoaded");
        }
    }
}