using Goog;
using GoogGUI.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogGUI
{
    [Panel(false, 100)]
    public class ClientSettings : IPanel, IFieldEditor
    {
        private bool _active;
        private Config _config;
        private List<IField> _fields;
        private List<RequiredCommand> _requiredActions = new List<RequiredCommand>();

        public ClientSettings(Config config)
        {
            _config = config;
            _config.FileSaved += OnConfigSaved;

            _fields = new List<IField>()
            {
            };
        }

        public event EventHandler? CanExecuteChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event PropertyChangingEventHandler? PropertyChanging;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                OnPropertyChanged("Active");
            }
        }

        public List<IField> Fields => _fields;

        public ImageSource Icon => new BitmapImage(new Uri(@"/Icons/Game.png", UriKind.Relative));

        public string Label => "Game Settings";

        public List<RequiredCommand> RequiredActions => _requiredActions;

        public DataTemplate Template => (DataTemplate)Application.Current.Resources["FieldEditor"];

        public bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid &&
                File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)) &&
                !string.IsNullOrEmpty(_config.ClientPath) &&
                File.Exists(Path.Combine(_config.ClientPath, Config.FolderGameBinaries, Config.FileClientBin));
        }

        public void Execute(object? parameter)
        {
            ((MainWindow)Application.Current.MainWindow).App.Panel = this;
        }

        private void OnConfigSaved(object? sender, Config e)
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnPropertyChanged(string v)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(v));
        }
    }
}