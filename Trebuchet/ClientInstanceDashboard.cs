using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Trebuchet
{
    public class ClientInstanceDashboard : INotifyPropertyChanged, IRecipient<ProcessMessage>
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
        private string _selectedModlist = string.Empty;
        private string _selectedProfile = string.Empty;

        public ClientInstanceDashboard()
        {
            KillCommand = new SimpleCommand(OnKilled, false);
            LaunchCommand = new TaskBlockedCommand(OnLaunched, true, Operations.SteamDownload);
            LaunchBattleEyeCommand = new TaskBlockedCommand(OnBattleEyeLaunched, true, Operations.SteamDownload);

            StrongReferenceMessenger.Default.Register<ProcessFailledMessage>(this);
            StrongReferenceMessenger.Default.Register<ProcessStartedMessage>(this);
            StrongReferenceMessenger.Default.Register<ProcessStoppedMessage>(this);

            _selectedProfile = App.Config.DashboardClientProfile;
            _selectedModlist = App.Config.DashboardClientModlist;

            Resolve();
            ListProfiles();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanUseDashboard => _config.IsInstallPathValid &&
                !string.IsNullOrEmpty(_config.ClientPath) &&
                File.Exists(Path.Combine(_config.ClientPath, Config.FolderGameBinaries, Config.FileClientBin));

        public SimpleCommand KillCommand { get; private set; }

        public TaskBlockedCommand LaunchBattleEyeCommand { get; private set; }

        public TaskBlockedCommand LaunchCommand { get; private set; }

        public List<string> Modlists { get; private set; } = new List<string>();

        public bool ProcessRunning { get; private set; }

        public ProcessStats ProcessStats { get; } = new ProcessStats();

        public List<string> Profiles { get; private set; } = new List<string>();

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                App.Config.DashboardClientModlist = _selectedModlist;
                App.Config.SaveFile();
            }
        }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                App.Config.DashboardClientProfile = _selectedProfile;
                App.Config.SaveFile();
            }
        }

        public void Kill()
        {
            if (!ProcessRunning) return;

            if (App.Config.DisplayWarningOnKill)
            {
                QuestionModal question = new QuestionModal("Kill", "Killing a process will trigger an abrupt ending of the program and can lead to Data loss and/or data corruption. " +
                    "Do you wish to continue ?");
                question.ShowDialog();
                if (question.Result != System.Windows.Forms.DialogResult.Yes) return;
            }

            KillCommand.Toggle(false);
            StrongReferenceMessenger.Default.Send(new KillProcessMessage(-1));
        }

        public void Launch(bool isBattleEye)
        {
            if (ProcessRunning) return;

            LaunchCommand.Toggle(false);
            LaunchBattleEyeCommand.Toggle(false);

            StrongReferenceMessenger.Default.Send(new CatapultClientMessage(SelectedProfile, SelectedModlist, isBattleEye));
        }

        void IRecipient<ProcessMessage>.Receive(ProcessMessage message)
        {
            if (message.instance >= 0) return;

            if (message is ProcessStartedMessage started)
                OnProcessStarted(started.data);
            else if (message is ProcessFailledMessage failed)
                OnProcessFailed(failed.Exception);
            else if (message is ProcessStoppedMessage stopped)
                OnProcessTerminated();
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
            OnPropertyChanged(nameof(Modlists));
            OnPropertyChanged(nameof(Profiles));
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

        private void OnProcessFailed(Exception exception)
        {
            KillCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            LaunchBattleEyeCommand.Toggle(true);
            new ErrorModal("Client failed to start", exception.Message).ShowDialog();
        }

        private void OnProcessStarted(ProcessData data)
        {
            LaunchCommand.Toggle(false);
            LaunchBattleEyeCommand.Toggle(false);
            KillCommand.Toggle(true);

            ProcessRunning = true;
            OnPropertyChanged(nameof(ProcessRunning));

            if (ProcessStats.Running) ProcessStats.StopStats();
            ProcessStats.StartStats(data, Path.GetFileNameWithoutExtension(Config.FileClientBin));
            StrongReferenceMessenger.Default.Send<ProcessStateChangedMessage>();
        }

        private void OnProcessTerminated()
        {
            ProcessStats.StopStats();
            KillCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            LaunchBattleEyeCommand.Toggle(true);

            ProcessRunning = false;
            OnPropertyChanged(nameof(ProcessRunning));
            StrongReferenceMessenger.Default.Send<ProcessStateChangedMessage>();
        }

        private void Resolve()
        {
            ClientProfile.ResolveProfile(_config, ref _selectedProfile);
            ModListProfile.ResolveProfile(_config, ref _selectedModlist);

            App.Config.DashboardClientModlist = _selectedModlist;
            App.Config.DashboardClientProfile = _selectedProfile;
            App.Config.SaveFile();
            OnPropertyChanged(nameof(SelectedModlist));
            OnPropertyChanged(nameof(SelectedProfile));
        }
    }
}