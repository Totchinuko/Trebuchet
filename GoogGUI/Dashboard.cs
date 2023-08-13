using Goog;
using GoogGUI.Attributes;
using GoogLib;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace GoogGUI
{
    [Panel("Dashboard", "/Icons/Dashboard.png", true, 0, "Dashboard")]
    public class Dashboard : Panel
    {
        private ClientInstanceDashboard _client;
        private ObservableCollection<ServerInstanceDashboard> _instances = new ObservableCollection<ServerInstanceDashboard>();
        private DispatcherTimer _timer;
        private Trebuchet _trebuchet;

        public Dashboard(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            CloseAllCommand = new SimpleCommand(OnCloseAll);
            KillAllCommand = new SimpleCommand(OnKillAll);
            LaunchAllCommand = new SimpleCommand(OnLaunchAll);

            _trebuchet = new Trebuchet(config);
            _trebuchet.DispatcherRequest += OnTrebuchetRequestDispatcher;
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, OnDispatcherTick, Application.Current.Dispatcher);
            _timer.Start();

            _client = new ClientInstanceDashboard(_config, _uiConfig, _trebuchet);
            FillServerInstances();
        }

        public ClientInstanceDashboard Client => _client;

        public SimpleCommand CloseAllCommand { get; private set; }

        public ObservableCollection<ServerInstanceDashboard> Instances => _instances;

        public SimpleCommand KillAllCommand { get; private set; }

        public SimpleCommand LaunchAllCommand { get; private set; }

        public bool CanDisplayServers => _config.IsInstallPathValid &&
                File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)) &&
                _config.ServerInstanceCount > 0;

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid;
        }

        public override void Execute(object? parameter)
        {
            if (CanExecute(parameter) && ((MainWindow)Application.Current.MainWindow).App.Panel != this)
            {
                _client.RefreshSelection();
                foreach (var i in _instances)
                    i.RefreshSelection();
                FillServerInstances();
            }
            base.Execute(parameter);
        }

        private void FillServerInstances()
        {
            if (_instances.Count >= _config.ServerInstanceCount)
            {
                OnPropertyChanged("Instances");
                return;
            }

            for (int i = _instances.Count; i < _config.ServerInstanceCount; i++)
                _instances.Add(new ServerInstanceDashboard(_config, _uiConfig, _trebuchet, i));
            OnPropertyChanged("Instances");
        }

        private void OnCloseAll(object? obj)
        {
            foreach (var i in Instances)
                i.Close();
        }

        private void OnDispatcherTick(object? sender, EventArgs e)
        {
            _trebuchet.TickTrebuchet();
        }

        private void OnKillAll(object? obj)
        {
            foreach (var i in Instances)
                i.Kill();            
        }

        private void OnLaunchAll(object? obj)
        {
            foreach (var i in Instances)
                i.Launch();
        }

        private void OnTrebuchetRequestDispatcher(object? sender, Action e)
        {
            Application.Current.Dispatcher.Invoke(e);
        }
    }
}