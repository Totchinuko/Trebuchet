using Goog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace GoogGUI
{
    public class GoogApp : INotifyPropertyChanged
    {
        private Config _config;
        private object? _panel;
        private bool _testlive;

        public GoogApp(bool testlive)
        {
            SettingsCommand = new SimpleCommand(DisplaySettings);
            ModlistCommand = new SimpleCommand(ModlistDisplay);
            _testlive = testlive;
            _config = Tools.LoadFile<Config>(Config.GetConfigPath(_testlive));

            if (!string.IsNullOrEmpty(_config.InstallPath) && !Tools.CanWriteHere(_config.InstallPath))
                new ErrorModal("Install Folder Error", "Cannot access the install folder", false).ShowDialog();

            if (string.IsNullOrEmpty(_config.InstallPath))
                new MessageModal("Install Folder", "In order to use Goog, please configure a folder to install your mods and profiles").ShowDialog();

            if (!_config.IsInstallPathValid)
                DisplaySettings(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanUseGame => CanUseModlist && !string.IsNullOrEmpty(_config.ClientPath) && File.Exists(Path.Combine(_config.ClientPath, Config.FolderGameBinaries, Config.FileClientBin));

        public bool CanUseModlist => _config.IsInstallPathValid && File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin));

        public bool CanUseServer => CanUseModlist;

        public ICommand ModlistCommand { get; private set; }

        public object? Panel { get => _panel; set => _panel = value; }

        public ICommand SettingsCommand { get; private set; }

        public void DisplaySettings(object? sender)
        {
            if (_panel is Settings) return;
            Settings setting = new Settings(_config);
            setting.ConfigChanged += OnConfigChanged;
            _panel = setting;
            OnPropertyChanged("Panel");
        }

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void InstallPrompt()
        {
        }

        private void ModlistDisplay(object? obj)
        {
            if (_panel is ModlistHandler) return;
            ModlistHandler handler = new ModlistHandler(_config);
            _panel = handler;
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