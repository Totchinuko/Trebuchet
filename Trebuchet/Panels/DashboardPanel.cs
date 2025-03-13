#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using TrebuchetLib;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

#endregion

namespace Trebuchet.Panels
{
    public class DashboardPanel : Panel,
        IRecipient<CatapulServersMessage>,
        IRecipient<DashboardStateChanged>,
        IRecipient<SteamModlistReceived>
    {
        private readonly Config _config;
        private bool _hasModRefreshScheduled;
        private readonly object _lock = new();
        private DispatcherTimer _timer;

        public DashboardPanel() : base("Dashboard")
        {
            _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
            CloseAllCommand = new SimpleCommand(OnCloseAll);
            KillAllCommand = new SimpleCommand(OnKillAll);
            LaunchAllCommand = new TaskBlockedCommand(OnLaunchAll, true, Operations.SteamDownload);
            UpdateServerCommand =
                new TaskBlockedCommand(OnServerUpdate, true, Operations.SteamDownload, Operations.ServerRunning);
            UpdateAllModsCommand = new TaskBlockedCommand(OnModUpdate, true, Operations.SteamDownload,
                Operations.GameRunning, Operations.ServerRunning);
            VerifyFilesCommand = new TaskBlockedCommand(OnFileVerification, true, Operations.SteamDownload,
                Operations.GameRunning, Operations.ServerRunning);

            Client = new ClientInstanceDashboard();
            CreateInstancesIfNeeded();

            StrongReferenceMessenger.Default.RegisterAll(this);

            _timer = new DispatcherTimer(TimeSpan.FromMinutes(5), DispatcherPriority.Background, OnCheckModUpdate);
        }

        public bool CanDisplayServers => _config is { IsInstallPathValid: true, ServerInstanceCount: > 0 };

        public ClientInstanceDashboard Client { get; }

        public SimpleCommand CloseAllCommand { get; private set; }

        public ObservableCollection<ServerInstanceDashboard> Instances { get; } = new();

        public SimpleCommand KillAllCommand { get; private set; }

        public TaskBlockedCommand LaunchAllCommand { get; private set; }

        public TaskBlockedCommand UpdateAllModsCommand { get; private set; }

        public TaskBlockedCommand UpdateServerCommand { get; private set; }

        public TaskBlockedCommand VerifyFilesCommand { get; private set; }

        public void Receive(CatapulServersMessage message)
        {
            StrongReferenceMessenger.Default.Send(new CatapultServerMessage(
                Instances.Where(i => !i.ProcessRunning).Select(i => (i.SelectedProfile, i.SelectedModlist, i.Instance))
            ));
        }

        public void Receive(DashboardStateChanged message)
        {
            if (Client.ProcessRunning)
            {
                if (StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.GameRunning))) return;
                StrongReferenceMessenger.Default.Send(new OperationStartMessage(Operations.GameRunning));
            }
            else
            {
                if (!StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.GameRunning))) return;
                StrongReferenceMessenger.Default.Send(new OperationReleaseMessage(Operations.GameRunning));
            }

            if (Instances.Any(i => i.ProcessRunning))
            {
                if (StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.ServerRunning))) return;
                StrongReferenceMessenger.Default.Send(new OperationStartMessage(Operations.ServerRunning));
            }
            else
            {
                if (!StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.ServerRunning))) return;
                StrongReferenceMessenger.Default.Send(new OperationReleaseMessage(Operations.ServerRunning));
            }
        }

        public void Receive(SteamModlistReceived message)
        {
            List<ulong> updates =
                StrongReferenceMessenger.Default.Send(
                    new SteamModlistUpdateRequest(message.Modlist.GetManifestKeyValuePairs()));
            var queried = message.Modlist.Select(x => x.PublishedFileID).ToList();

            Client.RefreshUpdateStatus(queried, updates);
            foreach (var item in Instances)
                item.RefreshUpdateStatus(queried, updates);
        }

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid &&
                   (Tools.IsClientInstallValid(_config) || Tools.IsServerInstallValid(_config));
        }

        /// <summary>
        ///     Collect all used modlists of all the client and server instances. Can have duplicates.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> CollectAllModlistNames()
        {
            if (Client.CanUseDashboard && !string.IsNullOrEmpty(Client.SelectedModlist))
                yield return Client.SelectedModlist;

            foreach (var i in Instances)
                if (i.CanUseDashboard && !string.IsNullOrEmpty(i.SelectedModlist))
                    yield return i.SelectedModlist;
        }

        /// <summary>
        ///     Show the panel.
        /// </summary>
        /// <param name="parameter">Unused</param>
        public override void Execute(object? parameter)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                throw new NotSupportedException("The application lifetime is not supported.");

            if (CanExecute(parameter) && (desktop.MainWindow as MainWindow)?.App?.ActivePanel != this)
            {
                Client.RefreshSelection();
                foreach (var i in Instances)
                    i.RefreshSelection();
                CreateInstancesIfNeeded();
                Task.Run(WaitModUpdateCheck);
            }

            base.Execute(parameter);
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
        }

        /// <summary>
        ///     Collect all used mods of all the client and server instances and update them. Will not perform any action if the
        ///     game is running or the main task is blocked.
        /// </summary>
        public void UpdateMods()
        {
            StrongReferenceMessenger.Default.Send(
                new ServerUpdateModsMessage(ModListProfile.CollectAllMods(_config, CollectAllModlistNames())
                    .Distinct()));
        }

        /// <summary>
        ///     Update all server instances. Will not perform any action if the game is running or the main task is blocked.
        /// </summary>
        public void UpdateServer()
        {
            StrongReferenceMessenger.Default.Send<ServerUpdateMessage>();
        }

        private void CheckModUpdates()
        {
            if (StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.SteamPublishedFilesFetch)))
                return;
            StrongReferenceMessenger.Default.Send(
                new SteamModlistIDRequest(ModListProfile.CollectAllMods(_config, CollectAllModlistNames()).Distinct()));
        }

        private void CreateInstancesIfNeeded()
        {
            if (Instances.Count >= _config.ServerInstanceCount)
            {
                OnPropertyChanged(nameof(Instances));
                return;
            }

            for (var i = Instances.Count; i < _config.ServerInstanceCount; i++)
                Instances.Add(new ServerInstanceDashboard(i));
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
            var question = new QuestionModal("Verify files",
                "This will verify all server and mod files. This may take a while. Do you want to continue?");
            question.OpenDialogue();
            if (!question.Result) return;

            StrongReferenceMessenger.Default.Send(
                new VerifyFilesMessage(ModListProfile.CollectAllMods(_config, CollectAllModlistNames()).Distinct()));
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

            while (StrongReferenceMessenger.Default.Send(
                       new OperationStateRequest(Operations.SteamPublishedFilesFetch)))
                await Task.Delay(200);

            Dispatcher.UIThread.Invoke(CheckModUpdates);

            lock (_lock)
                _hasModRefreshScheduled = false;
        }
    }
}