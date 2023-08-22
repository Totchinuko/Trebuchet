using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Trebuchet.Controls;
using Trebuchet.Messages;
using static SteamKit2.Internal.CMsgClientAMGetPersonaNameHistory;

namespace Trebuchet
{
    public class TrebuchetApp : INotifyPropertyChanged,
        IRecipient<SteamConnectMessage>,
        IRecipient<CatapultMessage>,
        IRecipient<ConfigRequest>,
        IRecipient<CloseProcessMessage>,
        IRecipient<ServerUpdateMessage>,
        IRecipient<VerifyFilesMessage>,
        IRecipient<InstanceInstalledCountRequest>,
        IRecipient<PanelActivateMessage>
    {
        private Panel? _activePanel;

        private bool _catapult;
        private TaskBlocker _taskBlocker = new TaskBlocker();
        private TrebuchetLauncher _trebuchet;

        public TrebuchetApp(bool testlive, bool catapult)
        {
            _trebuchet = new TrebuchetLauncher(testlive);
            _catapult = catapult;

            RegisterEvents();

            StrongReferenceMessenger.Default.Register<CatapultServerMessage>(this);
            StrongReferenceMessenger.Default.Register<CatapultClientMessage>(this);
            StrongReferenceMessenger.Default.Register<ConfigRequest>(this);
            StrongReferenceMessenger.Default.Register<CloseProcessMessage>(this);
            StrongReferenceMessenger.Default.Register<KillProcessMessage>(this);
            StrongReferenceMessenger.Default.Register<ShutdownProcessMessage>(this);
            StrongReferenceMessenger.Default.Register<ServerUpdateMessage>(this);
            StrongReferenceMessenger.Default.Register<VerifyFilesMessage>(this);
            StrongReferenceMessenger.Default.Register<InstanceInstalledCountRequest>(this);
            StrongReferenceMessenger.Default.Register<PanelActivateMessage>(this);

            var menuConfig = GuiExtensions.GetEmbededTextFile("Trebuchet.TrebuchetApp.Menu.json");
            Menu = JsonSerializer.Deserialize<Menu>(menuConfig) ?? throw new Exception("Could not deserialize the menu.");
            ActivePanel = Menu.Bottom.Where(x => x is Panel).Cast<Panel>().Where(x => x.CanExecute(null)).FirstOrDefault();

            _trebuchet.Steam.Connected += OnSteamConnected;
            _trebuchet.Steam.Disconnected += OnSteamDisconnected;
            _trebuchet.Steam.Connect();
            _trebuchet.Steam.SetProgress(SteamWidget);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

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

        public string AppTitle => $"Tot ! Trebuchet {GuiExtensions.GetFileVersion()}";

        public Menu Menu { get; set; } = new Menu();

        public SteamWidget SteamWidget { get; } = new SteamWidget();

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

        void IRecipient<ServerUpdateMessage>.Receive(ServerUpdateMessage message)
        {
            if (message is ServerUpdateModsMessage mods)
                UpdateServerMods(mods.modlist);
            else
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

        public void Receive(PanelActivateMessage message)
        {
            ActivePanel = message.panel;
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

        private bool Assert(bool assertion, string message)
        {
            if (!assertion)
            {
                new ErrorModal("Error", message).ShowDialog();
                return false;
            }
            return true;
        }

        private void CatchTask(Operations operation, Func<CancellationTokenSource, Task> action, Action? then)
        {
            var cts = _taskBlocker.Set(operation);
            Task.Run(async () =>
            {
                try
                {
                    await action.Invoke(cts);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        new ErrorModal("Error", $"{ex.Message + Environment.NewLine}Please check the log for more information.").ShowDialog();
                        return;
                    });
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _taskBlocker.Release(operation);
                    });
                }
                then?.Invoke();
            }, cts.Token);
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

        private void RegisterEvent<T>(T message) where T : ProcessMessage
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StrongReferenceMessenger.Default.Send(message);
            });
        }

        private void RegisterEvents()
        {
            _trebuchet.Launcher.ServerStarted += (_, e) => RegisterEvent(new ProcessStartedMessage(e.process, e.instance));
            _trebuchet.Launcher.ServerTerminated += (_, e) => RegisterEvent(new ProcessStoppedMessage(e));
            _trebuchet.Launcher.ServerFailed += (_, e) => RegisterEvent(new ProcessFailledMessage(e.Exception, e.Instance));

            _trebuchet.Launcher.ClientStarted += (_, e) => RegisterEvent(new ProcessStartedMessage(e.process));
            _trebuchet.Launcher.ClientTerminated += (_, e) => RegisterEvent(new ProcessStoppedMessage());
            _trebuchet.Launcher.ClientFailed += (_, e) => RegisterEvent(new ProcessFailledMessage(e.Exception));
        }

        private void UpdateServerMods(IEnumerable<ulong> modlist)
        {
            if (!Assert(!_taskBlocker.IsSet(Operations.GameRunning, Operations.SteamDownload), "Trebuchet is busy.")) return;
            if (!Assert(_trebuchet.Steam.IsConnected, "Steam is not available.")) return;

            SteamWidget.Start("Checking for mod updates...");
            CatchTask(Operations.SteamDownload, async (cts) =>
            {
                await _trebuchet.Steam.UpdateMods(modlist, cts);
            }, null);
        }

        private void UpdateServers()
        {
            if (!Assert(!_taskBlocker.IsSet(Operations.GameRunning, Operations.SteamDownload), "Trebuchet is busy.")) return;
            if (!Assert(_trebuchet.Steam.IsConnected, "Steam is not available.")) return;

            SteamWidget.Start("Checking for mod updates...");
            CatchTask(Operations.SteamDownload, async (cts) =>
            {
                await _trebuchet.Steam.UpdateServerInstances(cts);
            }, null);
        }

        private void UpdateThenCatapultClient(string profile, string modlist, bool isBattlEye)
        {
            if (!Assert(!_taskBlocker.IsSet(Operations.SteamDownload), "Trebuchet is busy.")) return;

            if (_trebuchet.Config.AutoUpdateStatus == AutoUpdateStatus.Never)
            {
                _trebuchet.Launcher.CatapultClient(profile, modlist, isBattlEye);
                return;
            }

            if (!Assert(_trebuchet.Steam.IsConnected, "Steam is not available.")) return;

            SteamWidget.Start("Checking mod files...");
            var allmodlist = ModListProfile.CollectAllMods(_trebuchet.Config, new string[] { modlist }).Distinct();
            CatchTask(Operations.SteamDownload, async (cts) =>
            {
                await _trebuchet.Steam.UpdateMods(allmodlist, cts);
            },
            () => _trebuchet.Launcher.CatapultClient(profile, modlist, isBattlEye));
        }

        private void UpdateThenCatapultServer(IEnumerable<(string profile, string modlist, int instance)> instances)
        {
            if (!Assert(!_taskBlocker.IsSet(Operations.SteamDownload), "Trebuchet is busy.")) return;

            if (_trebuchet.Config.AutoUpdateStatus == AutoUpdateStatus.Never || _trebuchet.Launcher.IsAnyServerRunning())
            {
                foreach (var (profile, modlist, instance) in instances)
                    _trebuchet.Launcher.CatapultServer(profile, modlist, instance);
                return;
            }

            if (!Assert(_trebuchet.Steam.IsConnected, "Steam is not available.")) return;

            SteamWidget.Start("Checking for server updates...");
            var allmodlist = ModListProfile.CollectAllMods(_trebuchet.Config, instances.Select(i => i.modlist)).Distinct();
            CatchTask(Operations.SteamDownload, async (cts) =>
            {
                await _trebuchet.Steam.UpdateServerInstances(cts);

                if (!_trebuchet.Launcher.IsClientRunning())
                {
                    SteamWidget.Report(0);
                    SteamWidget.SetDescription("Checking mod files...");
                    await _trebuchet.Steam.UpdateMods(allmodlist, cts);
                }
            },
            () =>
            {
                foreach (var (profile, modlist, instance) in instances)
                    _trebuchet.Launcher.CatapultServer(profile, modlist, instance);
            });
        }

        private void VerifyFiles(IEnumerable<ulong> modlist)
        {
            if (!Assert(!_taskBlocker.IsSet(Operations.GameRunning, Operations.SteamDownload), "Trebuchet is busy.")) return;
            if (!Assert(_trebuchet.Steam.IsConnected, "Steam is not available.")) return;

            SteamWidget.Start("Verifying server files...");

            CatchTask(Operations.SteamDownload, async (cts) =>
            {
                _trebuchet.Steam.ClearCache();
                await _trebuchet.Steam.UpdateServerInstances(cts);
                SteamWidget.Report(0);
                SteamWidget.SetDescription("Verifying mod files...");
                await _trebuchet.Steam.UpdateMods(modlist, cts);
            }, null);
        }
    }
}