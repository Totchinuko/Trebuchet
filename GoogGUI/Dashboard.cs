using Goog;
using GoogGUI.Attributes;
using GoogLib;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GoogGUI
{
    [Panel("Dashboard", "/Icons/Dashboard.png", true, 0, "Dashboard")]
    public class Dashboard : Panel
    {
        public const string GameTask = "GameRunning";
        public const string ModCheck = "ModCheck";
        private ClientInstanceDashboard _client;
        private ObservableCollection<ServerInstanceDashboard> _instances = new ObservableCollection<ServerInstanceDashboard>();
        private DispatcherTimer _timer;
        private Trebuchet _trebuchet;

        public Dashboard(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            CloseAllCommand = new SimpleCommand(OnCloseAll);
            KillAllCommand = new SimpleCommand(OnKillAll);
            LaunchAllCommand = new TaskBlockedCommand(OnLaunchAll);

            _trebuchet = new Trebuchet(config);
            _trebuchet.DispatcherRequest += OnTrebuchetRequestDispatcher;
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnDispatcherTick, Application.Current.Dispatcher);

            _client = new ClientInstanceDashboard(_config, _uiConfig, _trebuchet);
            FillServerInstances();

            OnDispatcherTick(this, EventArgs.Empty);
            _timer.Start();
        }

        public bool CanDisplayServers => _config.IsInstallPathValid &&
                File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin)) &&
                _config.ServerInstanceCount > 0;

        public ClientInstanceDashboard Client => _client;

        public SimpleCommand CloseAllCommand { get; private set; }

        public ObservableCollection<ServerInstanceDashboard> Instances => _instances;

        public SimpleCommand KillAllCommand { get; private set; }

        public TaskBlockedCommand LaunchAllCommand { get; private set; }

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid;
        }

        public override void Execute(object? parameter)
        {
            if (CanExecute(parameter) && ((MainWindow)Application.Current.MainWindow).App.ActivePanel != this)
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

        private void OnDetectModOutOfDate()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                throw new NotImplementedException();
            });
        }

        private void OnDispatcherTick(object? sender, EventArgs e)
        {
            _trebuchet.TickTrebuchet();

            if ((_trebuchet.IsClientRunning() || _trebuchet.IsAnyServerRunning()) && !App.TaskBlocker.IsSet(GameTask))
                App.TaskBlocker.Set(GameTask);

            if (!_trebuchet.IsClientRunning() && !_trebuchet.IsAnyServerRunning() && App.TaskBlocker.IsSet(GameTask))
                App.TaskBlocker.Release(GameTask);
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

        private void OnRunModCheck()
        {
            if (App.TaskBlocker.IsSet(ModCheck)) return;

            var modfiles = Tools.GetModFiles(_trebuchet.GetServerActiveWorkshopMods()).ToDictionary(k=>k.Key, v=>v.Value);

            var ct = App.TaskBlocker.Set(ModCheck);
            var query = new GetPublishedFileDetailsQuery(modfiles.Select(x => x.Key));
            Task.Run(() => SteamRemoteStorage.GetPublishedFileDetails(query, ct)).ContinueWith((x) => OnRunModCheckCompleted(x, modfiles));
        }

        private void OnRunModCheckCompleted(Task<PublishedFilesResponse> task, Dictionary<ulong, FileInfo> files)
        {
            if (task.Result.ResultCount == 0) return;
            foreach (PublishedFile published in task.Result.PublishedFileDetails)
            {
                if(files.TryGetValue(published.PublishedFileID, out FileInfo? info) &&
                    info.Exists && 
                    Tools.UnixTimeStampToDateTime(published.TimeUpdated) <= info.LastWriteTimeUtc) 
                    continue;

                OnDetectModOutOfDate();
                return;
            }
        }

        private void OnTrebuchetRequestDispatcher(object? sender, Action e)
        {
            Application.Current.Dispatcher.Invoke(e);
        }
    }
}