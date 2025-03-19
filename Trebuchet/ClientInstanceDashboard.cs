using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using TrebuchetLib;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet
{
    public class ClientInstanceDashboard : INotifyPropertyChanged, IRecipient<ClientProcessStateChanged>
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
        private string _selectedModlist = string.Empty;
        private string _selectedProfile = string.Empty;

        public ClientInstanceDashboard()
        {
            KillCommand = new SimpleCommand(OnKilled, false);
            LaunchCommand = new TaskBlockedCommand(OnLaunched, true, Operations.SteamDownload);
            LaunchBattleEyeCommand = new TaskBlockedCommand(OnBattleEyeLaunched, true, Operations.SteamDownload);
            UpdateModsCommand = new TaskBlockedCommand(OnModUpdate, true, Operations.SteamDownload, Operations.GameRunning, Operations.ServerRunning);

            StrongReferenceMessenger.Default.Register<ClientProcessStateChanged>(this);

            _selectedProfile = App.Config.DashboardClientProfile;
            _selectedModlist = App.Config.DashboardClientModlist;

            Resolve();
            ListProfiles();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanUseDashboard => _config.IsInstallPathValid &&
                !string.IsNullOrEmpty(_config.ClientPath) &&
                File.Exists(Path.Combine(_config.ClientPath, Config.FolderGameBinaries, Config.FileClientBin));

        public bool IsUpdateNeeded => UpdateNeeded.Count > 0;

        public SimpleCommand KillCommand { get; private set; }

        public TaskBlockedCommand LaunchBattleEyeCommand { get; private set; }

        public TaskBlockedCommand LaunchCommand { get; private set; }

        public List<string> Modlists { get; private set; } = new List<string>();

        public bool ProcessRunning { get; private set; }

        public IProcessStats ProcessStats { get; } = new ProcessStatsLight();

        public List<string> Profiles { get; private set; } = new List<string>();

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                CheckModUpdate();
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

        public TaskBlockedCommand UpdateModsCommand { get; private set; }

        public List<ulong> UpdateNeeded { get; private set; } = new List<ulong>();

        public async void Kill()
        {
            if (!ProcessRunning) return;

            if (App.Config.DisplayWarningOnKill)
            {
                //TODO: Add to AppText
                QuestionModal question = new QuestionModal("Kill", "Killing a process will trigger an abrupt ending of the program and can lead to Data loss and/or data corruption. " +
                    "Do you wish to continue ?");
                await question.OpenDialogueAsync();
                if (!question.Result) return;
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

        public void RefreshSelection()
        {
            Resolve();
            ListProfiles();
        }

        public void RefreshUpdateStatus(List<ulong> queried, List<ulong> updated)
        {
            if (CanUseDashboard && !string.IsNullOrEmpty(SelectedModlist))
            {
                var mods = ModListProfile.CollectAllMods(_config, SelectedModlist);
                UpdateNeeded = mods.Intersect(updated).Union(UpdateNeeded.Except(queried)).ToList();
                OnPropertyChanged(nameof(UpdateNeeded));
                OnPropertyChanged(nameof(IsUpdateNeeded));
            }
        }

        void IRecipient<ClientProcessStateChanged>.Receive(ClientProcessStateChanged message)
        {
            if (message.ProcessDetails.OldDetails.State.IsRunning() && !message.ProcessDetails.NewDetails.State.IsRunning())
                OnProcessTerminated(message.ProcessDetails.NewDetails);
            else if (!message.ProcessDetails.OldDetails.State.IsRunning() && message.ProcessDetails.NewDetails.State.IsRunning())
                OnProcessStarted(message.ProcessDetails.NewDetails);

            if (message.ProcessDetails.NewDetails.State == ProcessState.FAILED)
                OnProcessFailed();
            else if (ProcessRunning)
                ProcessStats.SetDetails(message.ProcessDetails.NewDetails);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void CheckModUpdate()
        {
            ClearUpdates();
            if (string.IsNullOrEmpty(SelectedModlist)) return;
            StrongReferenceMessenger.Default.Send(new SteamModlistIDRequest(ModListProfile.CollectAllMods(_config, SelectedModlist)));
        }

        private void ClearUpdates()
        {
            UpdateNeeded.Clear();
            OnPropertyChanged(nameof(UpdateNeeded));
            OnPropertyChanged(nameof(IsUpdateNeeded));
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

        private void OnModUpdate(object? obj)
        {
            if (string.IsNullOrEmpty(SelectedModlist)) return;
            StrongReferenceMessenger.Default.Send(new ServerUpdateModsMessage(ModListProfile.CollectAllMods(_config, SelectedModlist).Distinct()));
        }

        private async void OnProcessFailed()
        {
            KillCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            LaunchBattleEyeCommand.Toggle(true);
            await new ErrorModal("Client failed to start", "See the logs for more information.").OpenDialogueAsync();
        }

        private void OnProcessStarted(ProcessDetails details)
        {
            LaunchCommand.Toggle(false);
            LaunchBattleEyeCommand.Toggle(false);
            KillCommand.Toggle(true);

            ProcessRunning = true;
            OnPropertyChanged(nameof(ProcessRunning));

            ProcessStats.StartStats(details);
            StrongReferenceMessenger.Default.Send<DashboardStateChanged>();
        }

        private void OnProcessTerminated(ProcessDetails details)
        {
            ProcessStats.StopStats(details);
            KillCommand.Toggle(false);
            LaunchCommand.Toggle(true);
            LaunchBattleEyeCommand.Toggle(true);

            ProcessRunning = false;
            OnPropertyChanged(nameof(ProcessRunning));
            StrongReferenceMessenger.Default.Send<DashboardStateChanged>();
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