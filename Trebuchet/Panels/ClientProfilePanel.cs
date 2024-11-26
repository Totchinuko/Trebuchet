using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using TrebuchetGUILib;

namespace Trebuchet
{
    public class ClientProfilePanel : FieldEditorPanel
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
        private ClientProfile _profile;
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private string _selectedProfile;

        public ClientProfilePanel()
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
            return _config.IsInstallPathValid && Tools.IsClientInstallValid(_config);
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        protected override void BuildFields()
        {
            BuildFields("Trebuchet.Panels.ClientProfilePanel.Fields.json", this, "Profile");
        }

        protected override void OnValueChanged(string property)
        {
            _profile.SaveFile();
        }

        [MemberNotNull("_profile", "_selectedProfile")]
        private void LoadPanel()
        {
            MoveOriginalSavedFolder();
            _selectedProfile = App.Config.CurrentClientProfile;
            ClientProfile.ResolveProfile(_config, ref _selectedProfile);

            OnPropertyChanged(nameof(SelectedProfile));
            LoadProfileList();
            LoadProfile();
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = ClientProfile.LoadProfile(_config, ClientProfile.GetPath(_config, _selectedProfile));
            OnPropertyChanged(nameof(ProfileSize));
            RefreshFields();
        }

        private void LoadProfileList()
        {
            _profiles = new ObservableCollection<string>(ClientProfile.ListProfiles(_config));
            OnPropertyChanged(nameof(Profiles));
        }

        private void MoveOriginalSavedFolder()
        {
            if (string.IsNullOrEmpty(_config.ClientPath)) return;
            string savedFolder = Path.Combine(_config.ClientPath, Config.FolderGameSave);
            if (!Directory.Exists(savedFolder)) return;
            if (Tools.IsSymbolicLink(savedFolder)) return;

            ErrorModal question = new ErrorModal("Game Folder Reset", "Your game directory contain saved data from your previous use of the game. Game Directory has been reset, please set it up again.");
            question.ShowDialog();

            _config.ClientPath = string.Empty;
            _config.SaveFile();
            return;
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
            App.Config.CurrentClientProfile = _selectedProfile;
            App.Config.SaveFile();
            OnPropertyChanged(nameof(SelectedProfile));
            LoadProfile();
        }

        private void OnProfileCreate(object? obj)
        {
            InputTextModal modal = new InputTextModal("Create", "Profile Name");
            modal.ShowDialog();
            if (string.IsNullOrEmpty(modal.Text)) return;
            string name = modal.Text;
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
            if (question.Result)
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
            InputTextModal modal = new InputTextModal("Duplicate", "Profile Name");
            modal.SetValue(_selectedProfile);
            modal.ShowDialog();
            if (string.IsNullOrEmpty(modal.Text)) return;
            string name = modal.Text;
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