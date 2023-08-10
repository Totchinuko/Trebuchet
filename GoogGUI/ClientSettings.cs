using Goog;
using GoogGUI.Attributes;
using GoogLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogGUI
{
    [Panel("Game Settings", "/Icons/Game.png", false, 100)]
    public class ClientSettings : FieldEditorPanel
    {
        public ClientSettings(Config config) : base(config)
        {
            _config.FileSaved += OnConfigSaved;
        }

        #region Fields
        
        #endregion

        public string SelectedProfile
        {
            get => _config.CurrentClientProfile;
            set
            {
                _config.CurrentClientProfile = value;
                _config.SaveFile();
                OnPropertyChanged("SelectedProfile");
            }
        }

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid &&
                File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)) &&
                !string.IsNullOrEmpty(_config.ClientPath) &&
                File.Exists(Path.Combine(_config.ClientPath, Config.FolderGameBinaries, Config.FileClientBin));
        }

        private void OnConfigSaved(object? sender, Config e)
        {
            OnCanExecuteChanged();
        }
    }
}