using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Trebuchet.Assets;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet.ViewModels.Panels
{
    public class ClientProfilePanel : FieldEditorPanel
    {
        private readonly DialogueBox _box;
        private readonly AppSetup _setup;
        private readonly AppFiles _appFiles;
        private readonly UIConfig _uiConfig;
        private ClientProfile _profile;
        private ObservableCollection<string> _profiles = [];
        private string _selectedProfile;

        public ClientProfilePanel(
            DialogueBox box,
            AppSetup setup, 
            AppFiles appFiles, 
            UIConfig uiConfig) : 
            base(Resources.GameSaves, "mdi-controller", false)
        {
            _box = box;
            _setup = setup;
            _appFiles = appFiles;
            _uiConfig = uiConfig;
            LoadPanel();

            CreateProfileCommand.Subscribe(OnProfileCreate);
            DeleteProfileCommand.Subscribe(OnProfileDelete);
            DuplicateProfileCommand.Subscribe(OnProfileDuplicate);
            OpenFolderProfileCommand.Subscribe(OnOpenFolderProfile);
        }

        public SimpleCommand CreateProfileCommand { get; } = new();
        public SimpleCommand DeleteProfileCommand { get; } = new();
        public SimpleCommand DuplicateProfileCommand { get; } = new();
        public SimpleCommand OpenFolderProfileCommand { get; } = new();
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
            return Tools.IsClientInstallValid(_setup.Config);
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
            _profile = _appFiles.Client.Get(_selectedProfile);
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

        private async void OnProfileCreate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
            _profile = _appFiles.Client.Create(name);
            LoadProfileList();
            SelectedProfile = name;
        }

        private Validation ValidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Validation.Invalid(Resources.ErrorNameEmpty);
            if (_profiles.Contains(name))
                return Validation.Invalid(Resources.ErrorNameAlreadyTaken);
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return Validation.Invalid(Resources.ErrorNameInvalidCharacters);
            return Validation.Valid;
        }

        private async void OnProfileDelete()
        {
            if (string.IsNullOrEmpty(_selectedProfile)) return;

            OnBoardingConfirmation confirm = new OnBoardingConfirmation(
                    Resources.Deletion,
                    string.Format(Resources.DeletionText, _selectedProfile));
            await _box.OpenAsync(confirm);
            if (!confirm.Result) return;
            
            _appFiles.Client.Delete(_profile.ProfileName);

            string profile = string.Empty;
            profile = _appFiles.Client.ResolveProfile(profile);
            LoadProfileList();
            SelectedProfile = profile;
        }

        private async void OnProfileDuplicate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
            _profile = await _appFiles.Client.Duplicate(_profile.ProfileName, name);
            LoadProfileList();
            SelectedProfile = name;
        }

        private async Task<string?> GetNewProfileName()
        {
            var modal = new OnBoardingNameSelection(Resources.Create, string.Empty)
                .SetValidation(ValidateName);
            await _box.OpenAsync(modal);
            return modal.Value;
        }
    }
}