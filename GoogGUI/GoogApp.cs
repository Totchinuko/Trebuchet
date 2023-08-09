using Goog;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace GoogGUI
{
    public class GoogApp : INotifyPropertyChanged
    {
        private ClientSettings? _clientSettings;
        private Config _config;
        private ModlistHandler? _modlist;
        private object? _panel;
        private Settings? _settings;

        public GoogApp(Config config)
        {
            _config = config;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanUseGame => CanUseModlist && !string.IsNullOrEmpty(_config.ClientPath) && File.Exists(Path.Combine(_config.ClientPath, Config.FolderGameBinaries, Config.FileClientBin));

        public bool CanUseModlist => _config.IsInstallPathValid && File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin));

        public bool CanUseServer => CanUseModlist;

        public ICommand ClientSettingsCommand => new SimpleCommand(DisplayClientSettings);

        public ICommand ModlistCommand => new SimpleCommand(ModlistDisplay);

        public object? Panel { get => _panel; set => _panel = value; }

        public ICommand SettingsCommand => new SimpleCommand(DisplaySettings);

        public void BaseChecks()
        {
            if (!string.IsNullOrEmpty(_config.InstallPath) && !Tools.CanWriteHere(_config.InstallPath))
                new ErrorModal("Install Folder Error", "Cannot access the install folder", false).ShowDialog();

            if (string.IsNullOrEmpty(_config.InstallPath))
                new MessageModal("Install Folder", "In order to use Goog, please configure a folder to install your mods and profiles").ShowDialog();

            if (!_config.IsInstallPathValid)
                DisplaySettings(this);
        }

        public void DisplayClientSettings(object? sender)
        {
            if (_panel is ClientSettings) return;
            if (_clientSettings == null)
            {
                _clientSettings = new ClientSettings(_config);
            }
            _panel = _clientSettings;
            OnPropertyChanged("Panel");
        }

        public void DisplaySettings(object? sender)
        {
            if (_panel is Settings) return;
            if (_settings == null)
            {
                _settings = new Settings(_config);
                _settings.ConfigChanged += OnConfigChanged;
            }
            _panel = _settings;
            OnPropertyChanged("Panel");
        }

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void ModlistDisplay(object? obj)
        {
            if (_panel is ModlistHandler) return;
            if (_modlist == null)
            {
                _modlist = new ModlistHandler(_config);
            }
            _panel = _modlist;
            OnPropertyChanged("Panel");
        }

        private void OnConfigChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged("CanUseGame");
            OnPropertyChanged("CanUseServer");
            OnPropertyChanged("CanUseModlist");
        }
    }
}