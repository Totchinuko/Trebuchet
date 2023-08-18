using Goog;
using GoogLib;
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
            UpdateServerCommand = new TaskBlockedCommand(OnServerUpdate, true, TaskBlocker.MainTask, GameTask);
            UpdateAllModsCommand = new TaskBlockedCommand(OnModUpdate, true, TaskBlocker.MainTask, GameTask);

            _trebuchet = new Trebuchet(config);
            _trebuchet.DispatcherRequest += OnTrebuchetRequestDispatcher;
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnDispatcherTick, Application.Current.Dispatcher);

            _client = new ClientInstanceDashboard(_config, _uiConfig, _trebuchet);
            CreateInstancesIfNeeded();

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

        public TaskBlockedCommand UpdateAllModsCommand { get; private set; }

        public TaskBlockedCommand UpdateServerCommand { get; private set; }

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid;
        }

        /// <summary>
        /// Collect all used modlists of all the client and server instances. Can have duplicates.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> CollectAllModlistNames()
        {
            if (_client.CanUseDashboard && !string.IsNullOrEmpty(_client.SelectedModlist))
                yield return _client.SelectedModlist;

            foreach (var i in Instances)
                if (i.CanUseDashboard && !string.IsNullOrEmpty(i.SelectedModlist))
                    yield return i.SelectedModlist;
        }

        /// <summary>
        /// Collect all used mods of all the client and server instances. Can have duplicates.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ulong> CollectAllMods()
        {
            foreach (var i in CollectAllModlistNames().Distinct())
                if (ModListProfile.TryLoadProfile(_config, i, out ModListProfile? profile))
                    foreach (var m in profile.Modlist)
                        if (ModListProfile.TryParseModID(m, out ulong id))
                            yield return id;
        }

        /// <summary>
        /// Show the panel.
        /// </summary>
        /// <param name="parameter">Unused</param>
        public override void Execute(object? parameter)
        {
            if (CanExecute(parameter) && ((MainWindow)Application.Current.MainWindow).App.ActivePanel != this)
            {
                _client.RefreshSelection();
                foreach (var i in _instances)
                    i.RefreshSelection();
                CreateInstancesIfNeeded();
            }
            base.Execute(parameter);
        }

        /// <summary>
        /// Collect all used mods of all the client and server instances and update them. Will not perform any action if the game is running or the main task is blocked.
        /// </summary>
        public void UpdateMods()
        {
            if (App.TaskBlocker.IsSet(TaskBlocker.MainTask, GameTask)) return;
            var ct = App.TaskBlocker.SetMain("Update all selected modlists...");
            Task.Run(() => Setup.UpdateMods(_config, CollectAllMods().Distinct(), ct), ct)
                .ContinueWith((t) => Application.Current.Dispatcher.Invoke(() => CheckForCMDErrors(t)));
        }

        /// <summary>
        /// Update all server instances. Will not perform any action if the game is running or the main task is blocked.
        /// </summary>
        public void UpdateServer()
        {
            if (App.TaskBlocker.IsSet(TaskBlocker.MainTask, GameTask)) return;
            var ct = App.TaskBlocker.SetMain("Updating server instances...");
            Task.Run(() => Setup.UpdateServerInstances(_config, ct), ct)
                .ContinueWith((t) => Application.Current.Dispatcher.Invoke(() => CheckForCMDErrors(t)));
        }

        private void CheckForCMDErrors(Task<int> task)
        {
            if (task.IsFaulted || !task.IsCompleted || task.Exception != null || task.Result != 0)
            {
                App.TaskBlocker.ReleaseMain();
                new ErrorModal("SteamCMD Error", "SteamCMD has encountered an error. Please check the logs for more information.").ShowDialog();
            }
        }

        private void CreateInstancesIfNeeded()
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

        private void OnModUpdate(object? obj)
        {
            UpdateMods();
        }

        private void OnServerUpdate(object? obj)
        {
            UpdateServer();
        }

        private void OnTrebuchetRequestDispatcher(object? sender, Action e)
        {
            Application.Current.Dispatcher.Invoke(e);
        }
    }
}