using Goog;
using GoogLib;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    [Panel("Profiles", "/Icons/Game.png", false, 100, group: "Game")]
    public class ClientSettings : FieldEditorPanel
    {
        private ClientProfile _profile;
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private string _selectedProfile;

        public ClientSettings(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            LoadPanel();
        }

        public ICommand CreateProfileCommand => new SimpleCommand(OnProfileCreate);

        public ICommand DeleteProfileCommand => new SimpleCommand(OnProfileDelete);

        public ICommand DuplicateProfileCommand => new SimpleCommand(OnProfileDuplicate);

        public ICommand OpenFolderProfileCommand => new SimpleCommand(OnOpenFolderProfile);

        public ClientProfile Profile => _profile;

        public ObservableCollection<string> Profiles { get => _profiles; }

        public string ProfileSize => (Tools.DirectorySize(_profile.ProfileFolder) / 1024 / 1024).ToString() + "MB";

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

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        protected override void BuildFields()
        {
            BuildFields("GoogGUI.ClientSettings.Fields.json", this, "Profile");
        }

        protected override void OnValueChanged(string property)
        {
            _profile.SaveFile();
        }

        [MemberNotNull("_profile", "_selectedProfile")]
        private void LoadPanel()
        {
            MoveOriginalSavedFolder();
            _selectedProfile = _uiConfig.CurrentClientProfile;
            ClientProfile.ResolveProfile(_config, ref _selectedProfile);

            OnPropertyChanged("SelectedProfile");
            LoadProfileList();
            LoadProfile();
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = ClientProfile.LoadProfile(_config, ClientProfile.GetPath(_config, _selectedProfile));
            OnPropertyChanged("ProfileSize");
            RefreshFields();
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
            ClientProfile Original = ClientProfile.CreateProfile(_config, ClientProfile.GetPath(_config, "_Original"));
            Original.SaveFile();
            string profileFolder = Path.GetDirectoryName(Original.FilePath) ?? throw new DirectoryNotFoundException($"{Original.FilePath} path is invalid");
            Tools.DeepCopy(newPath, profileFolder);
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

            _profile = ClientProfile.CreateProfile(_config, Path.Combine(ClientProfile.GetPath(_config, name)));
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
            _profile = ClientProfile.LoadProfile(_config, path);
            _profile.SaveFile();
            LoadProfileList();
            SelectedProfile = name;
        }
    }
}