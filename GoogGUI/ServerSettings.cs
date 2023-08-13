using Goog;
using GoogGUI.Attributes;
using GoogLib;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
        private TrulyObservableCollection<ObservableString> _sudoList = new TrulyObservableCollection<ObservableString>();

        public ServerSettings(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            _config.FileSaved += OnConfigSaved;
            _uiConfig.FileSaved += OnUIConfigSaved;

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

        [IntField("Max Players", min: 0, defaultValue: 30, Sort = 10)]
        public int MaxPlayers
        {
            get => _profile.MaxPlayers;
            set
            {
                _profile.MaxPlayers = value;
                OnValueChanged();
            }
        }

        [StringListField("Sudo Super Admins", Sort = 20)]
        public TrulyObservableCollection<ObservableString> SudoSuperAdmins
        {
            get => _sudoList;
            set
            {
                if (_sudoList != null)
                    _sudoList.CollectionChanged -= OnSudoListChanged;
                _sudoList = value;
                if (_sudoList != null)
                    _sudoList.CollectionChanged += OnSudoListChanged;

                _profile.SudoSuperAdmins = ObservableString.ToList(value);
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

        [ToggleField("Use All Cores", false, Sort = 25)]
        public bool UseAllCores
        {
            get => _profile.UseAllCores;
            set
            {
                _profile.UseAllCores = value;
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
            SudoSuperAdmins = ObservableString.ToObservableList(_profile.SudoSuperAdmins);

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

        private void OnSudoListChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _profile.SudoSuperAdmins = ObservableString.ToList(_sudoList);
            OnValueChanged();
        }

        private void OnUIConfigSaved(object? sender, UIConfig e)
        {
            if (_uiConfig.CurrentServerProfile != _selectedProfile)
                SelectedProfile = _uiConfig.CurrentServerProfile;
        }


        private void OnValueChanged()
        {
            _profile.SaveFile();
        }
    }
}