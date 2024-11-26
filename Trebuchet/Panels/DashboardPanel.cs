using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using TrebuchetGUILib;

namespace Trebuchet
{
    public class DashboardPanel : Panel,
        IRecipient<CatapulServersMessage>,
        IRecipient<DashboardStateChanged>,
        IRecipient<SteamModlistReceived>
    {
        private ClientInstanceDashboard _client;
        private Config _config;
        private bool _hasModRefreshScheduled = false;
        private ObservableCollection<ServerInstanceDashboard> _instances = new ObservableCollection<ServerInstanceDashboard>();
        private object _lock = new object();
        private DispatcherTimer _timer;

        public DashboardPanel()
        {
            _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
            CloseAllCommand = new SimpleCommand(OnCloseAll);
            KillAllCommand = new SimpleCommand(OnKillAll);
            LaunchAllCommand = new TaskBlockedCommand(OnLaunchAll, true, Operations.SteamDownload);
            UpdateServerCommand = new TaskBlockedCommand(OnServerUpdate, true, Operations.SteamDownload, Operations.GameRunning);
            UpdateAllModsCommand = new TaskBlockedCommand(OnModUpdate, true, Operations.SteamDownload, Operations.GameRunning, Operations.ServerRunning);
            VerifyFilesCommand = new TaskBlockedCommand(OnFileVerification, true, Operations.SteamDownload, Operations.GameRunning, Operations.ServerRunning);

            _client = new ClientInstanceDashboard();
            CreateInstancesIfNeeded();

            StrongReferenceMessenger.Default.RegisterAll(this);

            _timer = new DispatcherTimer(TimeSpan.FromMinutes(5), DispatcherPriority.Background, OnCheckModUpdate, Application.Current.Dispatcher);
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

        public TaskBlockedCommand VerifyFilesCommand { get; private set; }

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid && (Tools.IsClientInstallValid(_config) || Tools.IsServerInstallValid(_config));
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
                Task.Run(WaitModUpdateCheck);
            }
            base.Execute(parameter);
        }

        public void Receive(CatapulServersMessage message)
        {
            StrongReferenceMessenger.Default.Send(new CatapultServerMessage(
                Instances.Where(i => !i.ProcessRunning).Select(i => (i.SelectedProfile, i.SelectedModlist, i.Instance))
                ));
        }

        public void Receive(DashboardStateChanged message)
        {
            if (_client.ProcessRunning || Instances.Any(i => i.ProcessRunning))
            {
                if (StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.GameRunning))) return;
                StrongReferenceMessenger.Default.Send(new OperationStartMessage(Operations.GameRunning));
            }
            else
            {
                if (!StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.GameRunning))) return;
                StrongReferenceMessenger.Default.Send(new OperationReleaseMessage(Operations.GameRunning));
            }
        }

        public void Receive(SteamModlistReceived message)
        {
            List<ulong> updates = StrongReferenceMessenger.Default.Send(new SteamModlistUpdateRequest(message.Modlist.GetManifestKeyValuePairs()));
            List<ulong> queried = message.Modlist.Select(x => x.PublishedFileID).ToList();

            _client.RefreshUpdateStatus(queried, updates);
            foreach (var item in _instances)
                item.RefreshUpdateStatus(queried, updates);
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
        }

        /// <summary>
        /// Collect all used mods of all the client and server instances and update them. Will not perform any action if the game is running or the main task is blocked.
        /// </summary>
        public void UpdateMods()
        {
            StrongReferenceMessenger.Default.Send(new ServerUpdateModsMessage(ModListProfile.CollectAllMods(_config, CollectAllModlistNames()).Distinct()));
        }

        /// <summary>
        /// Update all server instances. Will not perform any action if the game is running or the main task is blocked.
        /// </summary>
        public void UpdateServer()
        {
            StrongReferenceMessenger.Default.Send<ServerUpdateMessage>();
        }

        private void CheckModUpdates()
        {
            if (StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.SteamPublishedFilesFetch)))
                return;
            StrongReferenceMessenger.Default.Send(new SteamModlistIDRequest(ModListProfile.CollectAllMods(_config, CollectAllModlistNames()).Distinct()));
        }

        private void CreateInstancesIfNeeded()
        {
            if (_instances.Count >= _config.ServerInstanceCount)
            {
                OnPropertyChanged(nameof(Instances));
                return;
            }

            for (int i = _instances.Count; i < _config.ServerInstanceCount; i++)
                _instances.Add(new ServerInstanceDashboard(i));
            OnPropertyChanged(nameof(Instances));
        }

        private void OnCheckModUpdate(object? sender, EventArgs e)
        {
            if (!App.Config.AutoRefreshModlist) return;
            CheckModUpdates();
        }

        private void OnCloseAll(object? obj)
        {
            foreach (var i in Instances)
                i.Close();
        }

        private void OnFileVerification(object? obj)
        {
            var question = new QuestionModal("Verify files", "This will verify all server and mod files. This may take a while. Do you want to continue?");
            question.ShowDialog();
            if (!question.Result) return;

            StrongReferenceMessenger.Default.Send(new VerifyFilesMessage(ModListProfile.CollectAllMods(_config, CollectAllModlistNames()).Distinct()));
        }

        private void OnKillAll(object? obj)
        {
            foreach (var i in Instances)
                i.Kill();
        }

        private void OnLaunchAll(object? obj)
        {
            StrongReferenceMessenger.Default.Send(new CatapultServerMessage(
                Instances.Where(i => !i.ProcessRunning).Select(i => (i.SelectedProfile, i.SelectedModlist, i.Instance))
            ));
        }

        private void OnModUpdate(object? obj)
        {
            UpdateMods();
        }

        private void OnServerUpdate(object? obj)
        {
            UpdateServer();
        }

        private async Task WaitModUpdateCheck()
        {
            lock (_lock)
            {
                if (_hasModRefreshScheduled) return;
                _hasModRefreshScheduled = true;
            }

            while (StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.SteamPublishedFilesFetch)))
                await Task.Delay(200);

            Application.Current.Dispatcher.Invoke(() =>
            {
                CheckModUpdates();
            });

            lock (_lock)
                _hasModRefreshScheduled = false;
        }
    }
}