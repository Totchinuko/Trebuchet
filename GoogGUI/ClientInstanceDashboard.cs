using Goog;
using GoogLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace GoogGUI
{
    public class ClientInstanceDashboard : INotifyPropertyChanged
    {
        private Config _config;
        private string _selectedModlist = string.Empty;
        private string _selectedProfile = string.Empty;
        private Trebuchet _trebuchet;
        private UIConfig _uiConfig;

        public ClientInstanceDashboard(Config config, UIConfig uiConfig, Trebuchet trebuchet)
        {
            KillCommand = new SimpleCommand(OnKilled, false);
            LaunchCommand = new TaskBlockedCommand(OnLaunched);
            LaunchBattleEyeCommand = new TaskBlockedCommand(OnBattleEyeLaunched);

            _config = config;
            _trebuchet = trebuchet;
            _uiConfig = uiConfig;

            _trebuchet.ClientTerminated += OnProcessTerminated;
            _trebuchet.ClientStarted += OnProcessStarted;
            _trebuchet.ClientFailed += OnProcessFailed;

            _selectedProfile = _uiConfig.DashboardClientProfile;
            _selectedModlist = _uiConfig.DashboardClientModlist;

            Resolve();
            ListProfiles();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanUseDashboard => _config.IsInstallPathValid &&
                File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)) &&
                !string.IsNullOrEmpty(_config.ClientPath) &&
                File.Exists(Path.Combine(_config.ClientPath, Config.FolderGameBinaries, Config.FileClientBin));

        public SimpleCommand KillCommand { get; private set; }

        public TaskBlockedCommand LaunchBattleEyeCommand { get; private set; }

        public TaskBlockedCommand LaunchCommand { get; private set; }

        public List<string> Modlists { get; private set; } = new List<string>();

        public bool ProcessRunning => _trebuchet.IsClientRunning();

        public ProcessStats ProcessStats { get; } = new ProcessStats();

        public List<string> Profiles { get; private set; } = new List<string>();

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                _uiConfig.DashboardClientModlist = _selectedModlist;
                _uiConfig.SaveFile();
            }
        }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                _uiConfig.DashboardClientProfile = _selectedProfile;
                _uiConfig.SaveFile();
            }
        }

        public void Kill()
        {
            if (!_trebuchet.IsClientRunning()) return;

            if (_uiConfig.DisplayWarningOnKill)
            {
                QuestionModal question = new QuestionModal("Kill", "Killing a process will trigger an abrupt ending of the program and can lead to Data loss and/or data corruption. " +
                    "Do you wish to continue ?");
                question.ShowDialog();
                if (question.Result != System.Windows.Forms.DialogResult.Yes) return;
            }

            KillCommand.Toggle(false);
            _trebuchet.KillClient();
        }

        public void Launch(bool isBattleEye)
        {
            if (_trebuchet.IsClientRunning()) return;
            if (_trebuchet.IsFolderLocked(ClientProfile.GetFolder(_config, _selectedProfile)))
            {
                new ErrorModal("Locked", "This profile is currently used by another process. Only one process can use a profile at a time.").ShowDialog();
                return;
            }

            LaunchCommand.Toggle(false);
            LaunchBattleEyeCommand.Toggle(false);

            try
            {
                _trebuchet.CatapultClient(_selectedProfile, _selectedModlist, isBattleEye);
                OnPropertyChanged("ProcessRunning");
            }
            catch (Exception ex)
            {
                LaunchCommand.Toggle(true);
                LaunchBattleEyeCommand.Toggle(true);
                new ErrorModal("Error", ex.Message).ShowDialog();
            }
        }

        public void RefreshSelection()
        {
            Resolve();
            ListProfiles();
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ListProfiles()
        {
            Modlists = ModListProfile.ListProfiles(_config).ToList();
            Profiles = ClientProfile.ListProfiles(_config).ToList();
            OnPropertyChanged("Modlists");
            OnPropertyChanged("Profiles");
        }

        private void OnBattleEyeLaunched(object? obj)
        {
            Launch(true);
        }

        private void OnKilled(object? obj)
        {
            Kill();
        }

        private void OnLaunched(object? obj)
        {
            Launch(false);
        }

        private void OnProcessFailed(object? sender, TrebuchetFailEventArgs e)
        {
            new ErrorModal("Client failed to start", e.Exception.Message).ShowDialog();
        }

        private void OnProcessStarted(object? sender, TrebuchetStartEventArgs e)
        {
            LaunchCommand.Toggle(false);
            LaunchBattleEyeCommand.Toggle(false);
            KillCommand.Toggle(true);
            OnPropertyChanged("ProcessRunning");
            ProcessStats.StartStats(e.process, Path.GetFileNameWithoutExtension(Config.FileClientBin));
        }

        private void OnProcessTerminated(object? sender, EventArgs e)
        {
            ProcessStats.StopStats();
            KillCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            LaunchBattleEyeCommand.Toggle(true);
            OnPropertyChanged("ProcessRunning");
        }

        private void Resolve()
        {
            ClientProfile.ResolveProfile(_config, ref _selectedProfile);
            ModListProfile.ResolveProfile(_config, ref _selectedModlist);

            _uiConfig.DashboardClientModlist = _selectedModlist;
            _uiConfig.DashboardClientProfile = _selectedProfile;
            _uiConfig.SaveFile();
            OnPropertyChanged("SelectedModlist");
            OnPropertyChanged("SelectedProfile");
        }
    }
}