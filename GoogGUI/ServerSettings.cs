using Goog;
using GoogGUI.Attributes;
using GoogLib;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Security;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    [Panel("Settings", "/Icons/Server.png", false, 200, group: "Server")]
    public class ServerSettings : FieldEditorPanel
    {
        private ServerProfile _profile;
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private string _selectedProfile;

        public ServerSettings(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            _config.FileSaved += OnConfigSaved;

            _selectedProfile = _uiConfig.CurrentServerProfile;
            ServerProfile.ResolveProfile(_config, ref _selectedProfile);
            LoadProfileList();
            LoadProfile();
        }

        #region Fields
        [MapField("Game Map", "/Game/Maps/ConanSandbox/ConanSandbox", Sort = 0)]
        public string Map
        {
            get => _profile.Map;
            set
            {
                _profile.Map = value;
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
                   _config.ServerInstanceCount > 0;
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = ServerProfile.LoadFile(ServerProfile.GetPath(_config, _selectedProfile));

            if (Fields.Count == 0)
                BuildFields();
            else
                Fields.ForEach(field => field.RefreshValue());
        }

        private void LoadProfileList()
        {
            _profiles = new ObservableCollection<string>(ServerProfile.ListProfiles(_config));
            OnPropertyChanged("Profiles");
        }

        private void OnConfigSaved(object? sender, Config e)
        {
            OnCanExecuteChanged();

            if (_uiConfig.CurrentServerProfile != _selectedProfile)
                SelectedProfile = _uiConfig.CurrentServerProfile;
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
            _config.SaveFile();
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

            _profile = ServerProfile.CreateFile(Path.Combine(ServerProfile.GetPath(_config, name)));
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
            _profile = ServerProfile.LoadFile(path);
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