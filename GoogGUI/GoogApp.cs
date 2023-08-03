using Goog;
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

        public GoogApp()
        {
            SettingsCommand = new SimpleCommand(DisplaySettings);
            Config.Load(out _config, _testlive);

            //TODO - Modal for writing error
            //if(!Tools.CanWriteHere(_config.InstallPath))
            if (!_config.IsInstallPathValid)
            {
                _panel = new Settings(_config);
                return;
            }
            _profiles = _config.GetAllProfiles();
            _currentProfile = _config.CurrentProfile;
            LoadProfile();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CurrentProfile { get => _currentProfile; set => _currentProfile = value; }
        public bool IsProfileLoaded => _profile != null;
        public object? Panel { get => _panel; set => _panel = value; }
        public List<string> Profiles { get => _profiles; set => _profiles = value; }

        public ICommand SettingsCommand { get; private set; }

        public void DisplaySettings(object? sender)
        {
            Settings setting = new Settings(_config);
            _panel = setting;
            OnPropertyChanged("Panel");
        }

        public void LoadProfile()
        {
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

            try
            {
                Profile.Load(_testlive, _currentProfile, _config, out _profile);
                OnPropertyChanged("IsProfileLoaded");
            }
            catch
            {
                //TODO - Modal
            }
        }

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}