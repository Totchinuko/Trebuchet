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
    public class ServerProfilePanel : FieldEditorPanel
    {
        private readonly AppSetup _setup;
        private readonly AppFiles _appFiles;
        private readonly UIConfig _uiConfig;
        private ServerProfile _profile;
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private string _selectedProfile;
        private TrulyObservableCollection<ObservableString> _sudoList = new TrulyObservableCollection<ObservableString>();

        public ServerProfilePanel(
            AppSetup setup,
            AppFiles appFiles,
            UIConfig uiConfig
            ) : base(Resources.ServerSaves, "mdi-server-network", false)
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

        public ServerProfile Profile => _profile;

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
            return Tools.IsServerInstallValid(_setup.Config);
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        protected override void BuildFields()
        {
            BuildFields("Trebuchet.ViewModels.Panels.ServerProfilePanel.Fields.json", this, nameof(Profile));
        }

        protected override void OnValueChanged(string property)
        {
            _profile.SaveFile();
        }

        [MemberNotNull("_selectedProfile", "_profile")]
        private void LoadPanel()
        {
            _selectedProfile = _uiConfig.CurrentServerProfile;
            _selectedProfile = _appFiles.Server.ResolveProfile(_selectedProfile);

            OnPropertyChanged(nameof(SelectedProfile));
            LoadProfileList();
            LoadProfile();
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = _appFiles.Server.Get(_selectedProfile);
            OnPropertyChanged(nameof(ProfileSize));
            RefreshFields();
        }

        private void LoadProfileList()
        {
            _profiles = new ObservableCollection<string>(_appFiles.Server.ListProfiles());
            OnPropertyChanged(nameof(Profiles));
        }

        private void OnOpenFolderProfile(object? obj)
        {
            string? folder = Path.GetDirectoryName(_profile.FilePath);
            if (string.IsNullOrEmpty(folder)) return;
            Process.Start("explorer.exe", folder);
        }

        private void OnProfileChanged()
        {
            _selectedProfile = _appFiles.Server.ResolveProfile(_selectedProfile);
            _uiConfig.CurrentServerProfile = _selectedProfile;
            _uiConfig.SaveFile();
            OnPropertyChanged(nameof(SelectedProfile));
            LoadProfile();
        }

        private async void OnProfileCreate(object? obj)
        {
            InputTextModal modal = new InputTextModal(Resources.Create, Resources.ProfileName);
            await modal.OpenDialogueAsync();
            if (string.IsNullOrEmpty(modal.Text)) return;
            string name = modal.Text;
            if (_profiles.Contains(name))
            {
                await new ErrorModal(Resources.AlreadyExists, Resources.AlreadyExistsText).OpenDialogueAsync();
                return;
            }

            _profile = _appFiles.Server.Create(name);
            _profile.SaveFile();
            LoadProfileList();
            SelectedProfile = name;
        }

        private async void OnProfileDelete(object? obj)
        {
            if (string.IsNullOrEmpty(_selectedProfile)) return;

            QuestionModal question = new(Resources.Deletion, string.Format(Resources.DeletionText, _selectedProfile));
            await question.OpenDialogueAsync();
            if (question.Result)
            {
                _appFiles.Server.Delete(_profile.ProfileName);

                string profile = string.Empty;
                profile = _appFiles.Server.ResolveProfile(profile);
                LoadProfileList();
                SelectedProfile = profile;
            }
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

            _profile = await _appFiles.Server.Duplicate(_profile.ProfileName, name);
            LoadProfileList();
            SelectedProfile = name;
        }
    }
}