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
    public class ServerProfilePanel : FieldEditorPanel
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
        private ServerProfile _profile;
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private string _selectedProfile;
        private TrulyObservableCollection<ObservableString> _sudoList = new TrulyObservableCollection<ObservableString>();

        public ServerProfilePanel() : base("ServerSettings")
        {
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
            return _config.IsInstallPathValid && Tools.IsServerInstallValid(_config);
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        protected override void BuildFields()
        {
            BuildFields("Trebuchet.Panels.ServerProfilePanel.Fields.json", this, nameof(Profile));
        }

        protected override void OnValueChanged(string property)
        {
            _profile.SaveFile();
        }

        [MemberNotNull("_selectedProfile", "_profile")]
        private void LoadPanel()
        {
            _selectedProfile = App.Config.CurrentServerProfile;
            ServerProfile.ResolveProfile(_config, ref _selectedProfile);

            OnPropertyChanged(nameof(SelectedProfile));
            LoadProfileList();
            LoadProfile();
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = ServerProfile.LoadProfile(_config, ServerProfile.GetPath(_config, _selectedProfile));
            OnPropertyChanged(nameof(ProfileSize));
            RefreshFields();
        }

        private void LoadProfileList()
        {
            _profiles = new ObservableCollection<string>(ServerProfile.ListProfiles(_config));
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
            ServerProfile.ResolveProfile(_config, ref _selectedProfile);
            App.Config.CurrentServerProfile = _selectedProfile;
            App.Config.SaveFile();
            OnPropertyChanged(nameof(SelectedProfile));
            LoadProfile();
        }

        private void OnProfileCreate(object? obj)
        {
            InputTextModal modal = new InputTextModal("Create", "Profile Name");
            modal.OpenDialogue();
            if (string.IsNullOrEmpty(modal.Text)) return;
            string name = modal.Text;
            if (_profiles.Contains(name))
            {
                //TODO: Add to AppText
                new ErrorModal("Already Exists", "This profile name is already used").OpenDialogue();
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

            //TODO: Add to AppText
            QuestionModal question = new("Deletion", $"Do you wish to delete the selected profile {_selectedProfile} ?");
            question.OpenDialogue();
            if (question.Result)
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
            InputTextModal modal = new InputTextModal("Duplicate", "Profile Name");
            modal.SetValue(_selectedProfile);
            modal.OpenDialogue();
            if (string.IsNullOrEmpty(modal.Text)) return;
            string name = modal.Text;
            if (_profiles.Contains(name))
            {
                //TODO: Add to AppText
                new ErrorModal("Already Exists", "This profile name is already used").OpenDialogue();
                return;
            }

            string path = Path.Combine(ServerProfile.GetPath(_config, name));
            _profile.CopyFolderTo(path);
            _profile = ServerProfile.LoadProfile(_config, path);
            _profile.SaveFile();
            LoadProfileList();
            SelectedProfile = name;
        }
    }
}