using Goog;
using GoogGUI.Attributes;
using GoogLib;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    [Panel("Settings", "/Icons/Game.png", false, 100, group: "Game")]
    public class ClientSettings : FieldEditorPanel
    {
        private ClientProfile _profile;
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private string _selectedProfile;

        public ClientSettings(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            _config.FileSaved += OnConfigSaved;
            _uiConfig.FileSaved += OnUIConfigSaved;

            MoveOriginalSavedFolder();
            _selectedProfile = _uiConfig.CurrentClientProfile;
            ClientProfile.ResolveProfile(_config, ref _selectedProfile);
            LoadProfileList();
            LoadProfile();
        }

        #region Fields

        [IntSliderField("Texture Streaming Pool (MB)", 0, 4000, 0, Frequency = 100, Sort = 40)]
        public int AddedTexturePool
        {
            get => _profile.AddedTexturePool;
            set
            {
                _profile.AddedTexturePool = value;
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

        [ToggleField("Use Battle Eye", false, Sort = -10)]
        public bool UseBattleEye
        {
            get => _profile.UseBattleEye;
            set
            {
                _profile.UseBattleEye = value;
                OnValueChanged();
            }
        }

        [ToggleField("Better Texture on Ultra", false, Sort = 50)]
        public bool UltraAnisotropy
        {
            get => _profile.UltraAnisotropy;
            set
            {
                _profile.UltraAnisotropy = value;
                OnValueChanged();
            }
        }
        #endregion Fields

        public ICommand CreateProfileCommand => new SimpleCommand(OnProfileCreate);

        public ICommand DeleteProfileCommand => new SimpleCommand(OnProfileDelete);

        public ICommand DuplicateProfileCommand => new SimpleCommand(OnProfileDuplicate);

        public ICommand OpenFolderProfileCommand => new SimpleCommand(OnOpenFolderProfile);

        public ObservableCollection<string> Profiles { get => _profiles; }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                OnProfileChanged();
            }
        }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["ClientSettings"];

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

        private void LoadProfileList()
        {
            _profiles = new ObservableCollection<string>(ClientProfile.ListProfiles(_config));
            OnPropertyChanged("Profiles");
        }

        private void MoveOriginalSavedFolder()
        {
            if (string.IsNullOrEmpty(_config.ClientPath)) return;
            string savedFolder = Path.Combine(_config.ClientPath, Config.FolderGameSave);
            if (!Directory.Exists(savedFolder)) return;
            if (Tools.IsSymbolicLink(savedFolder)) return;

            QuestionModal question = new QuestionModal("Saved Data", "Your game directory contain saved data from your previous use of the game. " +
                "In order to use the features of the launcher, the folder will be renamed and its content copied into a new profile to use with the launcher. All your data will still be under the folder Saved_Original. " +
                "Do you wish to continue ?");
            question.ShowDialog();

            if (question.Result != System.Windows.Forms.DialogResult.Yes)
            {
                _config.ClientPath = string.Empty;
                _config.SaveFile();
                return;
            }

            string newPath = savedFolder + "_Original";
            Directory.Move(savedFolder, newPath);
            ClientProfile Original = ClientProfile.CreateFile(ClientProfile.GetPath(_config, "_Original"));
            Original.SaveFile();
            string profileFolder = Path.GetDirectoryName(Original.FilePath) ?? throw new DirectoryNotFoundException($"{Original.FilePath} path is invalid");
            Tools.DeepCopy(newPath, profileFolder);
        }

        private void OnConfigSaved(object? sender, Config e)
        {
            OnCanExecuteChanged();
            MoveOriginalSavedFolder();
        }

        private void OnOpenFolderProfile(object? obj)
        {
            string? folder = Path.GetDirectoryName(_profile.FilePath);
            if (string.IsNullOrEmpty(folder)) return;
            Process.Start("explorer.exe", folder);
        }

        private void OnProfileChanged()
        {
            ClientProfile.ResolveProfile(_config, ref _selectedProfile);
            _uiConfig.CurrentClientProfile = _selectedProfile;
            _uiConfig.SaveFile();
            OnPropertyChanged("SelectedProfile");
            LoadProfile();
        }

        private void OnProfileCreate(object? obj)
        {
            ChooseNameModal modal = new ChooseNameModal("Create", string.Empty);
            modal.ShowDialog();
            string name = modal.Name;
            if (string.IsNullOrEmpty(name)) return;
            if (_profiles.Contains(name))
            {
                new ErrorModal("Already Exitsts", "This profile name is already used").ShowDialog();
                return;
            }

            _profile = ClientProfile.CreateFile(Path.Combine(ClientProfile.GetPath(_config, name)));
            _profile.SaveFile();
            LoadProfileList();
            SelectedProfile = name;
        }

        private void OnProfileDelete(object? obj)
        {
            if (string.IsNullOrEmpty(_selectedProfile)) return;
            if (_profile == null) return;

            QuestionModal question = new QuestionModal("Deletion", $"Do you wish to delete the selected profile {_selectedProfile} ?");
            question.ShowDialog();
            if (question.Result == System.Windows.Forms.DialogResult.Yes)
            {
                _profile.DeleteFolder();

                string profile = string.Empty;
                ClientProfile.ResolveProfile(_config, ref profile);
                LoadProfileList();
                SelectedProfile = profile;
            }
        }

        private void OnProfileDuplicate(object? obj)
        {
            ChooseNameModal modal = new ChooseNameModal("Duplicate", _selectedProfile);
            modal.ShowDialog();
            string name = modal.Name;
            if (string.IsNullOrEmpty(name)) return;
            if (_profiles.Contains(name))
            {
                new ErrorModal("Already Exitsts", "This profile name is already used").ShowDialog();
                return;
            }

            string path = Path.Combine(ClientProfile.GetPath(_config, name));
            _profile.CopyFolderTo(path);
            _profile = ClientProfile.LoadFile(path);
            _profile.SaveFile();
            LoadProfileList();
            SelectedProfile = name;
        }

        private void OnValueChanged()
        {
            _profile.SaveFile();
        }

        private void OnUIConfigSaved(object? sender, UIConfig e)
        {
            if (_uiConfig.CurrentClientProfile != _selectedProfile)
                SelectedProfile = _uiConfig.CurrentClientProfile;
        }
    }
}