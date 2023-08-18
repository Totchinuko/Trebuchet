using Goog;
using GoogLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace GoogGUI
{
    public class ServerInstanceDashboard : INotifyPropertyChanged
    {
        private Config _config;
        private string _selectedModlist = string.Empty;
        private string _selectedProfile = string.Empty;
        private Trebuchet _trebuchet;
        private UIConfig _uiConfig;

        public ServerInstanceDashboard(Config config, UIConfig uiConfig, Trebuchet trebuchet, int instance)
        {
            KillCommand = new SimpleCommand(OnKilled, false);
            CloseCommand = new SimpleCommand(OnClose, false);
            LaunchCommand = new TaskBlockedCommand(OnLaunched);

            _config = config;
            _trebuchet = trebuchet;
            Instance = instance;
            _uiConfig = uiConfig;

            _trebuchet.ServerTerminated += OnProcessTerminated;
            _trebuchet.ServerStarted += OnProcessStarted;
            _trebuchet.ServerFailed += OnProcessFailed;

            _uiConfig.GetInstanceParameters(Instance, out _selectedModlist, out _selectedProfile);

            Resolve();
            ListProfiles();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanUseDashboard => _config.IsInstallPathValid &&
                File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)) &&
                (_config.ServerInstanceCount > Instance || ProcessRunning);

        public SimpleCommand CloseCommand { get; private set; }

        public int Instance { get; }

        public SimpleCommand KillCommand { get; private set; }

        public TaskBlockedCommand LaunchCommand { get; private set; }

        public List<string> Modlists { get; private set; } = new List<string>();

        public bool ProcessRunning => _trebuchet.IsServerRunning(Instance);

        public ProcessStats ProcessStats { get; } = new ProcessStats();

        public List<string> Profiles { get; private set; } = new List<string>();

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                _uiConfig.SetInstanceParameters(Instance, _selectedModlist, _selectedProfile);
                _uiConfig.SaveFile();
            }
        }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                _uiConfig.SetInstanceParameters(Instance, _selectedModlist, _selectedProfile);
                _uiConfig.SaveFile();
            }
        }

        public void Close()
        {
            if (!_trebuchet.IsServerRunning(Instance)) return;

            CloseCommand.Toggle(false);
            _trebuchet.CloseServer(Instance);
        }

        public void Kill()
        {
            if (!_trebuchet.IsServerRunning(Instance)) return;

            if (_uiConfig.DisplayWarningOnKill)
            {
                QuestionModal question = new QuestionModal("Kill", "Killing a process will trigger an abrupt ending of the program and can lead to Data loss and/or data corruption. " +
                    "Do you wish to continue ?");
                question.ShowDialog();
                if (question.Result != System.Windows.Forms.DialogResult.Yes) return;
            }

            KillCommand.Toggle(false);
            CloseCommand.Toggle(false);
            _trebuchet.KillServer(Instance);
        }

        public void Launch()
        {
            if (!CanUseDashboard) return;
            if (_trebuchet.IsServerRunning(Instance)) return;
            if (_trebuchet.IsFolderLocked(ServerProfile.GetFolder(_config, _selectedProfile)))
            {
                new ErrorModal("Locked", "This profile is currently used by another process. Only one process can use a profile at a time.").ShowDialog();
                return;
            }

            LaunchCommand.Toggle(false);
            try
            {
                _trebuchet.CatapultServer(_selectedProfile, _selectedModlist, Instance);
                OnPropertyChanged("ProcessRunning");
            }
            catch (Exception ex)
            {
                LaunchCommand.Toggle(true);
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
            Profiles = ServerProfile.ListProfiles(_config).ToList();
            OnPropertyChanged("Modlists");
            OnPropertyChanged("Profiles");
        }

        private void OnClose(object? obj)
        {
            Close();
        }

        private void OnKilled(object? obj)
        {
            Kill();
        }

        private void OnLaunched(object? obj)
        {
            Launch();
        }

        private void OnProcessFailed(object? sender, TrebuchetFailEventArgs e)
        {
            new ErrorModal("Server failed to start", e.Exception.Message).ShowDialog();
        }

        private void OnProcessStarted(object? sender, TrebuchetStartEventArgs e)
        {
            if (Instance != e.instance) return;

            LaunchCommand.Toggle(false);
            KillCommand.Toggle(true);
            CloseCommand.Toggle(true);
            OnPropertyChanged("ProcessRunning");

            if(ProcessStats.Running) ProcessStats.StopStats();
            ProcessStats.StartStats(e.process, Path.GetFileNameWithoutExtension(Config.FileServerBin));
        }

        private void OnProcessTerminated(object? sender, int instance)
        {
            if (Instance != instance) return;

            ProcessStats.StopStats();
            KillCommand.Toggle(false);
            CloseCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            OnPropertyChanged("ProcessRunning");
        }

        private void Resolve()
        {
            ServerProfile.ResolveProfile(_config, ref _selectedProfile);
            ModListProfile.ResolveProfile(_config, ref _selectedModlist);

            _uiConfig.SetInstanceParameters(Instance, _selectedModlist, _selectedProfile);
            _uiConfig.SaveFile();
            OnPropertyChanged("SelectedModlist");
            OnPropertyChanged("SelectedProfile");
        }
    }
}