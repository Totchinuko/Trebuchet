using Goog;
using GoogLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public class ServerInstanceDashboard : INotifyPropertyChanged
    {
        private Config _config;
        private int _instance;
        private List<string> _modlists = new List<string>();
        private ProcessStats _processStats = new ProcessStats();
        private List<string> _profiles = new List<string>();
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
            _instance = instance;
            _uiConfig = uiConfig;

            _trebuchet.ServerTerminated += OnProcessTerminated;
            _trebuchet.ServerProcessStarted += OnProcessStarted;

            _uiConfig.GetInstanceParameters(_instance, out _selectedModlist, out _selectedProfile);

            Resolve();
            ListProfiles();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanUseDashboard => _config.IsInstallPathValid &&
                File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)) &&
                (_config.ServerInstanceCount > _instance || ProcessRunning);

        public SimpleCommand CloseCommand { get; private set; }

        public int Instance => _instance;

        public SimpleCommand KillCommand { get; private set; }

        public TaskBlockedCommand LaunchCommand { get; private set; }

        public List<string> Modlists => _modlists;

        public bool ProcessRunning => _trebuchet.IsServerRunning(_instance);

        public ProcessStats ProcessStats => _processStats;

        public List<string> Profiles => _profiles;

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                _uiConfig.SetInstanceParameters(_instance, _selectedModlist, _selectedProfile);
                _uiConfig.SaveFile();
            }
        }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                _uiConfig.SetInstanceParameters(_instance, _selectedModlist, _selectedProfile);
                _uiConfig.SaveFile();
            }
        }

        public void Close()
        {
            if (!_trebuchet.IsServerRunning(_instance)) return;

            CloseCommand.Toggle(false);
            _trebuchet.CloseServer(_instance);
        }

        public void Kill()
        {
            if (!_trebuchet.IsServerRunning(_instance)) return;

            if (_uiConfig.DisplayWarningOnKill)
            {
                QuestionModal question = new QuestionModal("Kill", "Killing a process will trigger an abrupt ending of the program and can lead to Data loss and/or data corruption. " +
                    "Do you wish to continue ?");
                question.ShowDialog();
                if (question.Result != System.Windows.Forms.DialogResult.Yes) return;
            }

            KillCommand.Toggle(false);
            CloseCommand.Toggle(false);
            _trebuchet.KillServer(_instance);
        }

        public void Launch()
        {
            if (!CanUseDashboard) return;
            if (_trebuchet.IsServerRunning(_instance)) return;
            if (_trebuchet.IsFolderLocked(ServerProfile.GetFolder(_config, _selectedProfile)))
            {
                new ErrorModal("Locked", "This profile is currently used by another process. Only one process can use a profile at a time.").ShowDialog();
                return;
            }


            LaunchCommand.Toggle(false);
            try
            {
                _trebuchet.CatapultServer(_selectedProfile, _selectedModlist, _instance);
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
            _modlists = ModListProfile.ListProfiles(_config).ToList();
            _profiles = ServerProfile.ListProfiles(_config).ToList();
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

        private void OnProcessStarted(object? sender, TrebuchetStartEventArgs e)
        {
            if (_instance != e.instance) return;

            LaunchCommand.Toggle(false);
            KillCommand.Toggle(true);
            CloseCommand.Toggle(true);
            OnPropertyChanged("ProcessRunning");
            _processStats.StartStats(e.process, Path.GetFileNameWithoutExtension(Config.FileServerBin));
        }

        private void OnProcessTerminated(object? sender, int instance)
        {
            if (_instance != instance) return;

            _processStats.StopStats();
            KillCommand.Toggle(false);
            CloseCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            OnPropertyChanged("ProcessRunning");
        }

        private void Resolve()
        {
            ServerProfile.ResolveProfile(_config, ref _selectedProfile);
            ModListProfile.ResolveProfile(_config, ref _selectedModlist);

            _uiConfig.SetInstanceParameters(_instance, _selectedModlist, _selectedProfile);
            _uiConfig.SaveFile();
            OnPropertyChanged("SelectedModlist");
            OnPropertyChanged("SelectedProfile");
        }
    }
}