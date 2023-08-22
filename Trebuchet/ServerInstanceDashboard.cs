using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Trebuchet
{
    public class ServerInstanceDashboard : INotifyPropertyChanged, IRecipient<ProcessMessage>
    {
        private Config _config;
        private string _selectedModlist = string.Empty;
        private string _selectedProfile = string.Empty;

        public ServerInstanceDashboard(int instance)
        {
            KillCommand = new SimpleCommand(OnKilled, false);
            CloseCommand = new SimpleCommand(OnClose, false);
            LaunchCommand = new TaskBlockedCommand(OnLaunched, true, Operations.SteamDownload);

            _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
            Instance = instance;

            StrongReferenceMessenger.Default.Register<ProcessFailledMessage>(this);
            StrongReferenceMessenger.Default.Register<ProcessStartedMessage>(this);
            StrongReferenceMessenger.Default.Register<ProcessStoppedMessage>(this);

            App.Config.GetInstanceParameters(Instance, out _selectedModlist, out _selectedProfile);

            Resolve();
            ListProfiles();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanUseDashboard => _config.IsInstallPathValid &&
                (_config.ServerInstanceCount > Instance || ProcessRunning);

        public SimpleCommand CloseCommand { get; private set; }

        public int Instance { get; }

        public SimpleCommand KillCommand { get; private set; }

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
                App.Config.SetInstanceParameters(Instance, _selectedModlist, _selectedProfile);
                App.Config.SaveFile();
            }
        }

        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                App.Config.SetInstanceParameters(Instance, _selectedModlist, _selectedProfile);
                App.Config.SaveFile();
            }
        }

        public void Close()
        {
            if (!ProcessRunning) return;

            CloseCommand.Toggle(false);
            StrongReferenceMessenger.Default.Send(new CloseProcessMessage(Instance));
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
            CloseCommand.Toggle(false);
            StrongReferenceMessenger.Default.Send(new KillProcessMessage(Instance));
        }

        void IRecipient<ProcessMessage>.Receive(ProcessMessage message)
        {
            if (message.instance != Instance) return;

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
            Profiles = ServerProfile.ListProfiles(_config).ToList();
            OnPropertyChanged(nameof(Modlists));
            OnPropertyChanged(nameof(Profiles));
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
            if (!CanUseDashboard) return;
            if (ProcessRunning) return;

            LaunchCommand.Toggle(false);
            StrongReferenceMessenger.Default.Send(new CatapultServerMessage(new[]
            {
                (SelectedProfile, SelectedModlist, Instance)
            }));
        }

        private void OnProcessFailed(Exception exception)
        {
            KillCommand.Toggle(false);
            CloseCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            new ErrorModal("Server failed to start", exception.Message).ShowDialog();
        }

        private void OnProcessStarted(ProcessData data)
        {
            LaunchCommand.Toggle(false);
            KillCommand.Toggle(true);
            CloseCommand.Toggle(true);

            ProcessRunning = true;
            OnPropertyChanged(nameof(ProcessRunning));

            if (ProcessStats.Running) ProcessStats.StopStats();
            ProcessStats.StartStats(data, Path.GetFileNameWithoutExtension(Config.FileServerBin));
            StrongReferenceMessenger.Default.Send<ProcessStateChangedMessage>();
        }

        private void OnProcessTerminated()
        {
            ProcessStats.StopStats();
            KillCommand.Toggle(false);
            CloseCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            ProcessRunning = false;
            OnPropertyChanged(nameof(ProcessRunning));
            StrongReferenceMessenger.Default.Send<ProcessStateChangedMessage>();
        }

        private void Resolve()
        {
            ServerProfile.ResolveProfile(_config, ref _selectedProfile);
            ModListProfile.ResolveProfile(_config, ref _selectedModlist);

            App.Config.SetInstanceParameters(Instance, _selectedModlist, _selectedProfile);
            App.Config.SaveFile();
            OnPropertyChanged(nameof(SelectedModlist));
            OnPropertyChanged(nameof(SelectedProfile));
        }
    }
}