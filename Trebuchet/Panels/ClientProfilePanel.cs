using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using TrebuchetLib;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet.Panels
{
    public class ClientProfilePanel : FieldEditorPanel
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
        private ClientProfile _profile;
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private string _selectedProfile;

        public ClientProfilePanel() : base("ClientSettings")
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
            BuildFields("Trebuchet.Panels.ClientProfilePanel.Fields.json", this, nameof(Profile));
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

        private async void MoveOriginalSavedFolder()
        {
            if (string.IsNullOrEmpty(_config.ClientPath)) return;
            string savedFolder = Path.Combine(_config.ClientPath, Config.FolderGameSave);
            if (!Directory.Exists(savedFolder)) return;
            if (Tools.IsSymbolicLink(savedFolder)) return;

            ErrorModal question = new(App.GetAppText("GameFolderReset_Title"), App.GetAppText("GameFolderReset_Message"));
            await question.OpenDialogueAsync();

            _config.ClientPath = string.Empty;
            _config.SaveFile();
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

        private async void OnProfileCreate(object? obj)
        {
            InputTextModal modal = new(App.GetAppText("Create"), App.GetAppText("ProfileName"));
            await modal.OpenDialogueAsync();
            if (string.IsNullOrEmpty(modal.Text)) return;
            string name = modal.Text;
            if (_profiles.Contains(name))
            {
                await new ErrorModal(App.GetAppText("AlreadyExists"), App.GetAppText("AlreadyExists_Message")).OpenDialogueAsync();
                return;
            }

            _profile = ClientProfile.CreateProfile(_config, Path.Combine(ClientProfile.GetPath(_config, name)));
            _profile.SaveFile();
            LoadProfileList();
            SelectedProfile = name;
        }

        private async void OnProfileDelete(object? obj)
        {
            if (string.IsNullOrEmpty(_selectedProfile)) return;

            QuestionModal question = new(App.GetAppText("Deletion"), App.GetAppText("Deletion_Message", _selectedProfile));
            await question.OpenDialogueAsync();
            if (!question.Result) return;
            
            _profile.DeleteFolder();

            string profile = string.Empty;
            ClientProfile.ResolveProfile(_config, ref profile);
            LoadProfileList();
            SelectedProfile = profile;
        }

        private async void OnProfileDuplicate(object? obj)
        {
            InputTextModal modal = new InputTextModal(App.GetAppText("Duplicate"), App.GetAppText("ProfileName"));
            modal.SetValue(_selectedProfile);
            await modal.OpenDialogueAsync();
            if (string.IsNullOrEmpty(modal.Text)) return;
            string name = modal.Text;
            if (_profiles.Contains(name))
            {
                await new ErrorModal(App.GetAppText("AlreadyExists"), App.GetAppText("AlreadyExists_Message")).OpenDialogueAsync();
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