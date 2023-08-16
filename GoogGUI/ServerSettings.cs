using Goog;
using GoogGUI.Attributes;
using GoogLib;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    [Panel("Profiles", "/Icons/Server.png", false, 200, group: "Server")]
    public class ServerSettings : FieldEditorPanel
    {
        private ServerProfile _profile;
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private string _selectedProfile;
        private TrulyObservableCollection<ObservableString> _sudoList = new TrulyObservableCollection<ObservableString>();

        public ServerSettings(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            LoadPanel();
        }

        public ICommand CreateProfileCommand => new SimpleCommand(OnProfileCreate);

        public ICommand DeleteProfileCommand => new SimpleCommand(OnProfileDelete);

        public ICommand DuplicateProfileCommand => new SimpleCommand(OnProfileDuplicate);

        public ICommand OpenFolderProfileCommand => new SimpleCommand(OnOpenFolderProfile);

        public ServerProfile Profile => _profile;

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

        public string ProfileSize => (Tools.DirectorySize(_profile.ProfileFolder) / 1024 / 1024).ToString() + "MB";

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid &&
                   File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)) &&
                   _config.ServerInstanceCount > 0;
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        protected override void BuildFields()
        {
            BuildFields("GoogGUI.ServerSettings.Fields.json", this, "Profile");
        }

        [MemberNotNull("_selectedProfile", "_profile")]
        private void LoadPanel()
        {
            _selectedProfile = _uiConfig.CurrentServerProfile;
            ServerProfile.ResolveProfile(_config, ref _selectedProfile);

            OnPropertyChanged("SelectedProfile");
            LoadProfileList();
            LoadProfile();
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = ServerProfile.LoadProfile(_config, ServerProfile.GetPath(_config, _selectedProfile));
            OnPropertyChanged("ProfileSize");
            RefreshFields();
        }

        private void LoadProfileList()
        {
            _profiles = new ObservableCollection<string>(ServerProfile.ListProfiles(_config));
            OnPropertyChanged("Profiles");
        }

        private void OnOpenFolderProfile(object? obj)
        {
            string? folder = Path.GetDirectoryName(_profile.FilePath);
            if (string.IsNullOrEmpty(folder)) return;
            Process.Start("explorer.exe", folder);
        }

        private void OnProfileChanged()
        {
            ServerProfile.ResolveProfile(_config, ref _selectedProfile);
            _uiConfig.CurrentServerProfile = _selectedProfile;
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

            _profile = ServerProfile.CreateProfile(_config, Path.Combine(ServerProfile.GetPath(_config, name)));
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
                ServerProfile.ResolveProfile(_config, ref profile);
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

            string path = Path.Combine(ServerProfile.GetPath(_config, name));
            _profile.CopyFolderTo(path);
            _profile = ServerProfile.LoadProfile(_config, path);
            _profile.SaveFile();
            LoadProfileList();
            SelectedProfile = name;
        }

        private void OnValueChanged()
        {
            _profile.SaveFile();
        }
    }
}