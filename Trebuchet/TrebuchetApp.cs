using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using Trebuchet.Messages;
using Trebuchet.Utils;
using TrebuchetGUILib;
using TrebuchetLib;

namespace Trebuchet
{
    public class TrebuchetApp : INotifyPropertyChanged,
        IRecipient<SteamConnectMessage>,
        IRecipient<CatapultMessage>,
        IRecipient<ConfigRequest>,
        IRecipient<CloseProcessMessage>,
        IRecipient<ServerMessages>,
        IRecipient<VerifyFilesMessage>,
        IRecipient<InstanceInstalledCountRequest>,
        IRecipient<PanelActivateMessage>,
        IRecipient<ServerConsoleRequest>,
        IRecipient<ProcessServerDetailsRequest>,
        IRecipient<SteamModlistRequest>,
        IRecipient<SteamModlistUpdateRequest>,
        IRecipient<UACPromptRequest>,
        IRecipient<SteamModlistIDRequest>
    {
        private readonly TaskBlocker _taskBlocker = new();
        private readonly TrebuchetLauncher _trebuchet;
        private Panel? _activePanel;

        private bool _catapult;

        public TrebuchetApp(bool testlive, bool catapult)
        {
            _trebuchet = new TrebuchetLauncher(testlive);
            _catapult = catapult;

            RegisterEvents();

            StrongReferenceMessenger.Default.Register<SteamConnectMessage>(this);
            StrongReferenceMessenger.Default.Register<CatapultServerMessage>(this);
            StrongReferenceMessenger.Default.Register<CatapultClientMessage>(this);
            StrongReferenceMessenger.Default.Register<ConfigRequest>(this);
            StrongReferenceMessenger.Default.Register<CloseProcessMessage>(this);
            StrongReferenceMessenger.Default.Register<KillProcessMessage>(this);
            StrongReferenceMessenger.Default.Register<ShutdownProcessMessage>(this);
            StrongReferenceMessenger.Default.Register<ServerUpdateModsMessage>(this);
            StrongReferenceMessenger.Default.Register<ServerConsoleRequest>(this);
            StrongReferenceMessenger.Default.Register<ServerUpdateMessage>(this);
            StrongReferenceMessenger.Default.Register<VerifyFilesMessage>(this);
            StrongReferenceMessenger.Default.Register<InstanceInstalledCountRequest>(this);
            StrongReferenceMessenger.Default.Register<PanelActivateMessage>(this);
            StrongReferenceMessenger.Default.Register<ProcessServerDetailsRequest>(this);
            StrongReferenceMessenger.Default.Register<SteamModlistRequest>(this);
            StrongReferenceMessenger.Default.Register<SteamModlistUpdateRequest>(this);
            StrongReferenceMessenger.Default.Register<UACPromptRequest>(this);
            StrongReferenceMessenger.Default.Register<SteamModlistIDRequest>(this);

            if (!WriteAccessCheck()) return;

            var menuConfig = GuiExtensions.GetEmbededTextFile("Trebuchet.TrebuchetApp.Menu.json");
            Menu = JsonSerializer.Deserialize<Menu>(menuConfig) ?? throw new Exception("Could not deserialize the menu.");
            ActivePanel = Menu.Bottom.Where(x => x is Panel).Cast<Panel>().Where(x => x.CanExecute(null)).FirstOrDefault();

            _trebuchet.Steam.Connected += OnSteamConnected;
            _trebuchet.Steam.Disconnected += OnSteamDisconnected;
            _trebuchet.Steam.Connect();
            Steam.SetProgress(SteamWidget);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static string AppTitle => $"Tot ! Trebuchet {GuiExtensions.GetFileVersion()}";
        public Panel? ActivePanel
        {
            get => _activePanel;
            set
            {
                if (_activePanel != null)
                    _activePanel.Active = false;
                _activePanel = value;
                if (_activePanel != null)
                    _activePanel.Active = true;
                OnPropertyChanged(nameof(ActivePanel));
            }
        }
        public Menu Menu { get; set; } = new Menu();

        public SteamWidget SteamWidget { get; } = new SteamWidget();

        public void OnWindowShow()
        {
            Menu.Top.ForEach(x => x.OnWindowShow());
            Menu.Bottom.ForEach(x => x.OnWindowShow());
        }

        public void Receive(PanelActivateMessage message)
        {
            ActivePanel = message.panel;
        }

        public void Receive(ServerConsoleRequest message)
        {
            message.Reply(_trebuchet.Launcher.GetServerConsole(message.instance));
        }

        public void Receive(ProcessServerDetailsRequest message)
        {
            message.Reply(_trebuchet.Launcher.GetServersDetails().ToList());
        }

        public void Receive(SteamModlistRequest message)
        {
            RequestModlistManifests(message.modlist);
        }

        public void Receive(SteamModlistUpdateRequest message)
        {
            var updated = _trebuchet.Steam.GetUpdatedUGCFileIDs(message.keyValuePairs).ToList();
            foreach (var (PubID, _) in message.keyValuePairs)
            {
                string mod = PubID.ToString();
                if (!ModListProfile.ResolveMod(_trebuchet.Config, ref mod) && !updated.Contains(PubID))
                    updated.Add(PubID);
            }
            message.Reply(updated);
        }

        public void Receive(UACPromptRequest message)
        {
            QuestionModal modal = new(
                App.GetAppText("UACDialog_Title"),
                App.GetAppText("UACDialog", message.Directory)
                );
            modal.ShowDialog();

            if (modal.Result)
            {
                GuiExtensions.RestartProcess(_trebuchet.Config.IsTestLive, true);
                message.Reply(true);
            }
            else
                message.Reply(false);
        }

        public void Receive(SteamModlistIDRequest message)
        {
            RequestModlistManifests(message.Modlist.ToList());
        }

        void IRecipient<SteamConnectMessage>.Receive(SteamConnectMessage message)
        {
            _trebuchet.Steam.Connect();
        }

        void IRecipient<ConfigRequest>.Receive(ConfigRequest message)
        {
            message.Reply(_trebuchet.Config);
        }

        void IRecipient<CatapultMessage>.Receive(CatapultMessage message)
        {
            if (_taskBlocker.IsSet(Operations.SteamDownload)) return;

            if (message is CatapultClientMessage client)
                UpdateThenCatapultClient(client.profile, client.modlist, client.isBattleEye);
            else if (message is CatapultServerMessage server)
                UpdateThenCatapultServer(server.instances);
        }

        void IRecipient<CloseProcessMessage>.Receive(CloseProcessMessage message)
        {
            if (message.instance >= 0)
                if (message is ShutdownProcessMessage)
                    throw new NotImplementedException();
                else if (message is KillProcessMessage)
                    _trebuchet.Launcher.KillServer(message.instance);
                else
                    _trebuchet.Launcher.CloseServer(message.instance);
            else
                _trebuchet.Launcher.KillClient();
        }

        void IRecipient<ServerMessages>.Receive(ServerMessages message)
        {
            if (message is ServerUpdateModsMessage mods)
                UpdateServerMods(mods.modlist);
            else if (message is ServerUpdateMessage)
                UpdateServers();
        }

        void IRecipient<VerifyFilesMessage>.Receive(VerifyFilesMessage message)
        {
            VerifyFiles(message.modlist);
        }

        void IRecipient<InstanceInstalledCountRequest>.Receive(InstanceInstalledCountRequest message)
        {
            message.Reply(_trebuchet.Steam.GetInstalledInstances());
        }

        internal virtual void OnAppClose()
        {
            _trebuchet.Launcher.Dispose();
            _trebuchet.Steam.Disconnect();
            Task.Run(() =>
            {
                while (_trebuchet.Steam.IsConnected)
                    Task.Delay(100);
            }).Wait();
        }

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private static void RegisterEvent<T>(T message) where T : ProcessStateChanged
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StrongReferenceMessenger.Default.Send(message);
            });
        }

        private static void RequestModlistManifests(List<ulong> list)
        {
            if (!GuiExtensions.Assert(!StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.SteamPublishedFilesFetch)), "Trebuchet is busy.")) return;
            if (list.Count == 0) return;

            new CatchedTasked(Operations.SteamPublishedFilesFetch, 15 * 1000)
                .Add(async (cts) =>
                {
                    PublishedFilesResponse? result = null;
                    result = await SteamRemoteStorage.GetPublishedFileDetails(new GetPublishedFileDetailsQuery(list), cts.Token);
                    if (result == null) return;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StrongReferenceMessenger.Default.Send(new SteamModlistReceived(result));
                    });
                }
                ).Start();
        }

        private void OnSteamConnected(object? sender, EventArgs e)
        {
            StrongReferenceMessenger.Default.Send(new SteamConnectionChangedMessage(true));
            if (_catapult)
            {
                _catapult = false;
                StrongReferenceMessenger.Default.Send<CatapulServersMessage>();
            }
        }

        private void OnSteamDisconnected(object? sender, EventArgs e)
        {
            StrongReferenceMessenger.Default.Send(new SteamConnectionChangedMessage(false));
        }

        private void RegisterEvents()
        {
            _trebuchet.Launcher.ServerProcessStateChanged += (_, e) => RegisterEvent(new ServerProcessStateChanged(e));
            _trebuchet.Launcher.ClientProcessStateChanged += (_, e) => RegisterEvent(new ClientProcessStateChanged(e));
        }

        private void RequestModlistManifests(string modlistName)
        {
            if (!ModListProfile.TryLoadProfile(_trebuchet.Config, modlistName, out ModListProfile? profile)) return;

            RequestModlistManifests(profile.GetModIDList().ToList());
        }

        private void UpdateServerMods(IEnumerable<ulong> modlist)
        {
            if (!GuiExtensions.Assert(!_taskBlocker.IsSet(Operations.ServerRunning, Operations.SteamDownload), "Trebuchet is busy.")) return;
            //if (!GuiExtensions.Assert(_trebuchet.Steam.IsConnected, "Steam is not available.")) return;

            SteamWidget.Start("Updating mods...");
            new CatchedTasked(Operations.SteamDownload)
                .Add(async (cts) =>
                {
                    await _trebuchet.Steam.UpdateMods(modlist, cts);
                })
                .Then(() =>
                {
                    RequestModlistManifests(modlist.ToList());
                })
                .Start();
        }

        private void UpdateServers()
        {
            if (!GuiExtensions.Assert(!_taskBlocker.IsSet(Operations.ServerRunning, Operations.SteamDownload), "Trebuchet is busy.")) return;
            //if (!GuiExtensions.Assert(_trebuchet.Steam.IsConnected, "Steam is not available.")) return;

            SteamWidget.Start("Updating servers...");
            new CatchedTasked(Operations.SteamDownload)
                .Add(_trebuchet.Steam.UpdateServerInstances)
                .Then(() => StrongReferenceMessenger.Default.Send<PanelRefreshConfigMessage>())
                .Start();
        }

        private void UpdateThenCatapultClient(string profile, string modlist, bool isBattlEye)
        {
            if (!GuiExtensions.Assert(!_taskBlocker.IsSet(Operations.SteamDownload), "Trebuchet is busy.")) return;

            if (_trebuchet.Config.AutoUpdateStatus == AutoUpdateStatus.Never || _trebuchet.Launcher.IsAnyServerRunning())
            {
                _trebuchet.Launcher.CatapultClient(profile, modlist, isBattlEye);
                return;
            }

            //if (!GuiExtensions.Assert(_trebuchet.Steam.IsConnected, "Steam is not available.")) return;

            SteamWidget.Start("Checking mod files...");
            var allmodlist = ModListProfile.CollectAllMods(_trebuchet.Config, [modlist]).Distinct();
            new CatchedTasked(Operations.SteamDownload)
                .Add(async (cts) =>
                {
                    await _trebuchet.Steam.UpdateMods(allmodlist, cts);
                })
                .Then(() => _trebuchet.Launcher.CatapultClient(profile, modlist, isBattlEye))
                .Start();
        }

        private void UpdateThenCatapultServer(IEnumerable<(string profile, string modlist, int instance)> instances)
        {
            if (!GuiExtensions.Assert(!_taskBlocker.IsSet(Operations.SteamDownload), "Trebuchet is busy.")) return;

            if (_trebuchet.Config.AutoUpdateStatus == AutoUpdateStatus.Never || _trebuchet.Launcher.IsAnyServerRunning())
            {
                foreach (var (profile, modlist, instance) in instances)
                    _trebuchet.Launcher.CatapultServer(profile, modlist, instance);
                return;
            }

            //if (!GuiExtensions.Assert(_trebuchet.Steam.IsConnected, "Steam is not available.")) return;

            SteamWidget.Start("Checking for server updates...");
            var allmodlist = ModListProfile.CollectAllMods(_trebuchet.Config, instances.Select(i => i.modlist)).Distinct();
            new CatchedTasked(Operations.SteamDownload)
                .Add(async (cts) =>
                {
                    await _trebuchet.Steam.UpdateServerInstances(cts);

                    if (!_trebuchet.Launcher.IsClientRunning())
                    {
                        SteamWidget.Report(0);
                        SteamWidget.SetDescription("Checking mod files...");
                        await _trebuchet.Steam.UpdateMods(allmodlist, cts);
                    }
                })
                .Then(() =>
                {
                    foreach (var (profile, modlist, instance) in instances)
                        _trebuchet.Launcher.CatapultServer(profile, modlist, instance);
                })
                .Start();
        }

        private void VerifyFiles(IEnumerable<ulong> modlist)
        {
            if (!GuiExtensions.Assert(!_taskBlocker.IsSet(Operations.GameRunning, Operations.SteamDownload, Operations.ServerRunning), "Trebuchet is busy.")) return;
            //if (!GuiExtensions.Assert(_trebuchet.Steam.IsConnected, "Steam is not available.")) return;

            SteamWidget.Start("Verifying server files...");

            new CatchedTasked(Operations.SteamDownload)
                .Add(async (cts) =>
                {
                    _trebuchet.Steam.ClearCache();
                    await _trebuchet.Steam.UpdateServerInstances(cts);
                    SteamWidget.Report(0);
                    SteamWidget.SetDescription("Verifying mod files...");
                    await _trebuchet.Steam.UpdateMods(modlist, cts);
                })
                .Start();
        }

        private bool WriteAccessCheck()
        {
            if (GuiExtensions.ValidateInstallDirectory(_trebuchet.Config.ResolvedInstallPath, out string _) && !Tools.ValidateDirectoryUAC(_trebuchet.Config.ResolvedInstallPath))
            {
                GuiExtensions.RestartProcess(_trebuchet.Config.IsTestLive, true);
                return false;
            }
            if (GuiExtensions.ValidateGameDirectory(_trebuchet.Config.ClientPath, out string _) && !Tools.ValidateDirectoryUAC(_trebuchet.Config.ClientPath))
            {
                GuiExtensions.RestartProcess(_trebuchet.Config.IsTestLive, true);
                return false;
            }
            return true;
        }
    }
}