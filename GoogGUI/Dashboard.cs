using Goog;
using GoogGUI.Attributes;
using GoogLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace GoogGUI
{
    [Panel("Dashboard", "/Icons/Dashboard.png", true, 0, "Dashboard")]
    public class Dashboard : Panel
    {
        private List<string> _clientProfiles = new List<string>();
        private ProcessStats _clientStats;
        private ObservableCollection<InstanceDashboard> _instances = new ObservableCollection<InstanceDashboard>();
        private List<string> _modlists = new List<string>();
        private DispatcherTimer _timer;
        private Trebuchet _trebuchet;

        public Dashboard(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            ClientKillCommand = new SimpleCommand(OnClientKilled, false);
            ClientLaunchCommand = new TaskBlockedCommand(OnClientLaunched);

            _trebuchet = new Trebuchet(config);
            _trebuchet.ClientTerminated += OnClientTerminated;
            _trebuchet.ServerTerminated += OnServerTerminated;
            _trebuchet.DispatcherRequest += OnTrebuchetRequestDispatcher;
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, OnDispatcherTick, Application.Current.Dispatcher);
            _timer.Start();

            _clientStats = new ProcessStats();

            if (_trebuchet.ClientProcess?.Process != null)
            {
                _clientStats.SetProcess(_trebuchet.ClientProcess.Process);
                ClientKillCommand.Toggle(true);
                ClientLaunchCommand.Toggle(false);
            }

            Resolve();
            ListProfiles();
        }

        public bool CanLaunchClient => _config.IsInstallPathValid &&
                File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)) &&
                !string.IsNullOrEmpty(_config.ClientPath) &&
                File.Exists(Path.Combine(_config.ClientPath, Config.FolderGameBinaries, Config.FileClientBin));

        public SimpleCommand ClientKillCommand { get; private set; }

        public TaskBlockedCommand ClientLaunchCommand { get; private set; }

        public List<string> ClientProfiles => _clientProfiles;

        public bool ClientRunning => _trebuchet.ClientProcess != null;

        public ProcessStats ClientStats => _clientStats;

        public ObservableCollection<InstanceDashboard> Instances => _instances;

        public List<string> Modlists => _modlists;

        public string SelectClientModlist
        {
            get => _uiConfig.DashboardClientModlist;
            set
            {
                _uiConfig.DashboardClientModlist = value;
                _uiConfig.SaveFile();
            }
        }

        public string SelectedClientProfile
        {
            get => _uiConfig.DashboardClientProfile;
            set
            {
                _uiConfig.DashboardClientProfile = value;
                _uiConfig.SaveFile();
            }
        }

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid;
        }

        public override void Execute(object? parameter)
        {
            if (CanExecute(parameter) && ((MainWindow)Application.Current.MainWindow).App.Panel != this)
            {
                Resolve();
                ListProfiles();
            }
            base.Execute(parameter);
        }

        private void ListProfiles()
        {
            _modlists = ModListProfile.ListProfiles(_config);
            _clientProfiles = ClientProfile.ListProfiles(_config);
            OnPropertyChanged("Modlists");
            OnPropertyChanged("ClientProfiles");
        }

        private void OnClientKilled(object? obj)
        {
            if (_trebuchet.ClientProcess == null) return;

            ClientKillCommand.Toggle(false);
            _trebuchet.ClientProcess.Kill();
        }

        private void OnClientLaunched(object? obj)
        {
            if (_trebuchet.ClientProcess != null) return;

            ClientLaunchCommand.Toggle(false);

            Process process = _trebuchet.CatapultClient(_uiConfig.DashboardClientProfile, _uiConfig.DashboardClientModlist);
            _clientStats.SetProcess(process);

            ClientKillCommand.Toggle(true);
            OnPropertyChanged("ClientRunning");
        }

        private void OnClientTerminated(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ClientKillCommand.Toggle(false);
                ClientLaunchCommand.Toggle(true);
                OnPropertyChanged("ClientRunning");
            });
        }

        private void OnDispatcherTick(object? sender, EventArgs e)
        {
            _trebuchet.TickTrebuchet();
        }

        private void OnServerTerminated(object? sender, int e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
            });
        }

        private void OnTrebuchetRequestDispatcher(object? sender, Action e)
        {
            Application.Current.Dispatcher.Invoke(e);
        }

        private void Resolve()
        {
            string profile = _uiConfig.DashboardClientProfile;
            ClientProfile.ResolveProfile(_config, ref profile);
            _uiConfig.DashboardClientProfile = profile;

            profile = _uiConfig.DashboardClientModlist;
            ModListProfile.ResolveProfile(_config, ref profile);
            _uiConfig.DashboardClientModlist = profile;

            _uiConfig.SaveFile();

            OnPropertyChanged("SelectClientModlist");
            OnPropertyChanged("SelectedClientProfile");
        }
    }
}