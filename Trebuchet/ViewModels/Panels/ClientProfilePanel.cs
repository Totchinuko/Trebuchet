using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Input;
using Trebuchet.Assets;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet.ViewModels.Panels
{
    public class ClientProfilePanel : FieldEditorPanel
    {
        private readonly AppSetup _setup;
        private readonly AppFiles _appFiles;
        private readonly UIConfig _uiConfig;
        private ClientProfile _profile;
        private ObservableCollection<string> _profiles = [];
        private string _selectedProfile;

        public ClientProfilePanel(AppSetup setup, AppFiles appFiles, UIConfig uiConfig) : base(Resources.GameSaves, "ClientSettings", "mdi-controller", false)
        {
            _setup = setup;
            _appFiles = appFiles;
            _uiConfig = uiConfig;
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
            return _setup.Config.IsInstallPathValid && Tools.IsClientInstallValid(_setup.Config);
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        protected override void BuildFields()
        {
            BuildFields("Trebuchet.ViewModels.Panels.ClientProfilePanel.Fields.json", this, nameof(Profile));
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
            _selectedProfile = _appFiles.Client.ResolveProfile(_selectedProfile);

            OnPropertyChanged(nameof(SelectedProfile));
            LoadProfileList();
            LoadProfile();
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = ClientProfile.LoadProfile(_appFiles.Client.GetPath(_selectedProfile));
            OnPropertyChanged(nameof(ProfileSize));
            RefreshFields();
        }

        private void LoadProfileList()
        {
            _profiles = new ObservableCollection<string>(_appFiles.Client.ListProfiles());
            OnPropertyChanged(nameof(Profiles));
        }

        private async void MoveOriginalSavedFolder()
        {
            if (string.IsNullOrEmpty(_setup.Config.ClientPath)) return;
            string savedFolder = Path.Combine(_setup.Config.ClientPath, Constants.FolderGameSave);
            if (!Directory.Exists(savedFolder)) return;
            if (Tools.IsSymbolicLink(savedFolder)) return;

            ErrorModal question = new(Resources.GameFolderReset, Resources.GameFolderResetText);
            await question.OpenDialogueAsync();

            _setup.Config.ClientPath = string.Empty;
            _setup.Config.SaveFile();
        }

        private void OnOpenFolderProfile(object? obj)
        {
            string? folder = Path.GetDirectoryName(_profile.FilePath);
            if (string.IsNullOrEmpty(folder)) return;
            Process.Start("explorer.exe", folder);
        }

        private void OnProfileChanged()
        {
            _selectedProfile = _appFiles.Client.ResolveProfile(_selectedProfile);
            _uiConfig.CurrentClientProfile = _selectedProfile;
            _uiConfig.SaveFile();
            OnPropertyChanged(nameof(SelectedProfile));
            LoadProfile();
        }

        private async void OnProfileCreate(object? obj)
        {
            InputTextModal modal = new(Resources.Create, Resources.ProfileName);
            await modal.OpenDialogueAsync();
            if (string.IsNullOrEmpty(modal.Text)) return;
            string name = modal.Text;
            if (_profiles.Contains(name))
            {
                await new ErrorModal(Resources.AlreadyExists, Resources.AlreadyExistsText).OpenDialogueAsync();
                return;
            }

            _profile = ClientProfile.CreateProfile(_appFiles.Client.GetPath(name));
            _profile.SaveFile();
            LoadProfileList();
            SelectedProfile = name;
        }

        private async void OnProfileDelete(object? obj)
        {
            if (string.IsNullOrEmpty(_selectedProfile)) return;

            QuestionModal question = new(Resources.Deletion, string.Format(Resources.DeletionText, _selectedProfile));
            await question.OpenDialogueAsync();
            if (!question.Result) return;
            
            _profile.DeleteFolder();

            string profile = string.Empty;
            profile = _appFiles.Client.ResolveProfile(profile);
            LoadProfileList();
            SelectedProfile = profile;
        }

        private async void OnProfileDuplicate(object? obj)
        {
            InputTextModal modal = new InputTextModal(Resources.Duplicate, Resources.ProfileName);
            modal.SetValue(_selectedProfile);
            await modal.OpenDialogueAsync();
            if (string.IsNullOrEmpty(modal.Text)) return;
            string name = modal.Text;
            if (_profiles.Contains(name))
            {
                await new ErrorModal(Resources.AlreadyExists, Resources.AlreadyExistsText).OpenDialogueAsync();
                return;
            }

            string path = Path.Combine(_appFiles.Client.GetPath(name));
            _profile.CopyFolderTo(path);
            _profile = ClientProfile.LoadProfile(path);
            _profile.SaveFile();
            LoadProfileList();
            SelectedProfile = name;
        }
    }
}