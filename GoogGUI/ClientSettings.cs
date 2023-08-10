using Goog;
using GoogGUI.Attributes;
using GoogLib;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace GoogGUI
{
    [Panel("Game Settings", "/Icons/Game.png", false, 100)]
    public class ClientSettings : FieldEditorPanel
    {
        private ClientProfile _profile;
        private string _selectedProfile;

        public ClientSettings(Config config) : base(config)
        {
            _config.FileSaved += OnConfigSaved;

            _selectedProfile = _config.CurrentClientProfile;
            ClientProfile.ResolveProfile(_config, ref _selectedProfile);
            LoadProfile();
        }

        #region Fields
        [ToggleField("Remove Intro Video", false, Sort = 0)]
        public bool RemoveIntroVideo
        {
            get => _profile.RemoveIntroVideo;
            set
            {
                _profile.RemoveIntroVideo = value;
                OnValueChanged();
            }
        }

        [ToggleField("Background Sound", false, Sort = 10)]
        public bool BackgroundSound
        {
            get => _profile.BackgroundSound;
            set
            {
                _profile.BackgroundSound = value;
                OnValueChanged();
            }
        }

        [ToggleField("Use All Cores", false, Sort = 20)]
        public bool UseAllCores
        {
            get => _profile.UseAllCores;
            set
            {
                _profile.UseAllCores = value;
                OnValueChanged();
            }
        }

        [ToggleField("Display Log Console", false, Sort = 30)]
        public bool Log
        {
            get => _profile.Log;
            set
            {
                _profile.Log = value;
                OnValueChanged();
            }
        }

        [IntSliderField("Texture Streaming Pool", 0, 4000, 0, Frequency = 100, Sort = 40)]
        public int AddedTexturePool
        {
            get => _profile.AddedTexturePool;
            set
            {
                _profile.AddedTexturePool = value;
                OnValueChanged();
            }
        }
        #endregion Fields

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                OnProfileChanged();
            }
        }

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid &&
                File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)) &&
                !string.IsNullOrEmpty(_config.ClientPath) &&
                File.Exists(Path.Combine(_config.ClientPath, Config.FolderGameBinaries, Config.FileClientBin));
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = ClientProfile.LoadFile(ClientProfile.GetPath(_config, _selectedProfile));

            if (Fields.Count == 0)
                BuildFields();
            else
                Fields.ForEach(field => field.RefreshValue());
        }

        private void OnConfigSaved(object? sender, Config e)
        {
            OnCanExecuteChanged();
        }

        private void OnProfileChanged()
        {
            string selected = _config.CurrentClientProfile;
            ClientProfile.ResolveProfile(_config, ref selected);
            _config.CurrentClientProfile = selected;
            _config.SaveFile();
            OnPropertyChanged("SelectedProfile");
            LoadProfile();
        }

        private void OnValueChanged()
        {
            _profile.SaveFile();
        }
    }
}