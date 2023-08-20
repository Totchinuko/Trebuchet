using Goog;
using GoogLib;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GoogGUI
{
    public class Dashboard : Panel
    {
        public const string GameTask = "GameRunning";
        public const string ModCheck = "ModCheck";
        private ClientInstanceDashboard _client;
        private ObservableCollection<ServerInstanceDashboard> _instances = new ObservableCollection<ServerInstanceDashboard>();
        private DispatcherTimer _timer;
        private Trebuchet _trebuchet;
        private SteamSession _steam;

        public Dashboard(Config config, UIConfig uiConfig, SteamSession steam, Trebuchet trebuchet) : base(config, uiConfig)
        {
            CloseAllCommand = new SimpleCommand(OnCloseAll);
            KillAllCommand = new SimpleCommand(OnKillAll);
            LaunchAllCommand = new TaskBlockedCommand(OnLaunchAll);
            UpdateServerCommand = new TaskBlockedCommand(OnServerUpdate, true, TaskBlocker.MainTask, GameTask);
            UpdateAllModsCommand = new TaskBlockedCommand(OnModUpdate, true, TaskBlocker.MainTask, GameTask);

            _steam = steam;
            _trebuchet = trebuchet;
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnDispatcherTick, Application.Current.Dispatcher);

            _client = new ClientInstanceDashboard(_config, _uiConfig, _trebuchet);
            CreateInstancesIfNeeded();

            OnDispatcherTick(this, EventArgs.Empty);
            _timer.Start();

            if (App.ImmediateServerCatapult)
            {
                Log.Write("Immediate server catapult requested.", LogSeverity.Info);
                foreach (var i in Instances)
                    i.Launch();
            }
            _steam = steam;
        }

        public bool CanDisplayServers => _config.IsInstallPathValid &&
                _config.ServerInstanceCount > 0;

        public ClientInstanceDashboard Client => _client;

        public SimpleCommand CloseAllCommand { get; private set; }

        public ObservableCollection<ServerInstanceDashboard> Instances => _instances;

        public SimpleCommand KillAllCommand { get; private set; }

        public TaskBlockedCommand LaunchAllCommand { get; private set; }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["Dashboard"];

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
            var cts = App.TaskBlocker.SetMain("Update all selected modlists...");

            Task.Run(async () =>
            {
                try
                {
                    await Setup.UpdateMods(_config, _steam, CollectAllMods().Distinct(), cts);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        new ErrorModal("Mod update failed", $"Mod update failed. Please check the log for more information. ({ex.Message})").ShowDialog();
                    });
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(App.TaskBlocker.ReleaseMain);
                }
            }, cts.Token);
        }

        /// <summary>
        /// Update all server instances. Will not perform any action if the game is running or the main task is blocked.
        /// </summary>
        public void UpdateServer()
        {
            if (App.TaskBlocker.IsSet(TaskBlocker.MainTask, GameTask)) return;
            var cts = App.TaskBlocker.SetMain("Updating server instances...");

            Task.Run(async () =>
            {
                try
                {
                    await Setup.UpdateServerInstances(_config, _steam, cts);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        new ErrorModal("Mod update failed", $"Mod update failed. Please check the log for more information. ({ex.Message})").ShowDialog();
                    });
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(App.TaskBlocker.ReleaseMain);
                }
            }, cts.Token);
        }

        private void CreateInstancesIfNeeded()
        {
            if (_instances.Count >= _config.ServerInstanceCount)
            {
                OnPropertyChanged("Instances");
                return;
            }

            for (int i = _instances.Count; i < _config.ServerInstanceCount; i++)
            {
                var inst = new ServerInstanceDashboard(_config, _uiConfig, _trebuchet, i);
                inst.LaunchRequested += OnServerLaunchRequested;
                _instances.Add(inst);
            }
            OnPropertyChanged("Instances");
        }

        private void LaunchServer(int instance)
        {
            if (instance >= _instances.Count)
                throw new ArgumentOutOfRangeException(nameof(instance));

            if (instance < 0)
                foreach (var i in _instances)
                    i.Launch();
            else
                _instances[instance].Launch();
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
            OnServerLaunchRequested(this, -1);
        }

        private void OnModUpdate(object? obj)
        {
            UpdateMods();
        }

        private void OnServerLaunchRequested(object? sender, int instance)
        {
            if (sender is not ServerInstanceDashboard dashboard)
                throw new InvalidOperationException();

            if (_instances.Any(i => i.ProcessRunning) || !_config.AutoUpdateOnStart)
            {
                LaunchServer(instance);
                return;
            }

            // TODO: Update then start
        }

        private void OnServerUpdate(object? obj)
        {
            UpdateServer(); 
        }
    }
}