using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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

            RegisterMessages();

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
                _trebuchet.Launcher.CatapultClient(client.profile, client.modlist, client.isBattleEye);
            else if (message is CatapultServerMessage server)
                UpdateThenCatapultServer(server.profile, server.modlist, server.instance);
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

        private void RegisterMessages()
        {
            _trebuchet.Launcher.ServerStarted += (_, e) => StrongReferenceMessenger.Default.Send(new ProcessStartedMessage(e.process, e.instance));
            _trebuchet.Launcher.ServerTerminated += (_, e) => StrongReferenceMessenger.Default.Send(new ProcessStoppedMessage(e));
            _trebuchet.Launcher.ServerFailed += (_, e) => StrongReferenceMessenger.Default.Send(new ProcessFailledMessage(e.Exception, e.Instance));
            StrongReferenceMessenger.Default.RegisterAll(this);
        }

        private void UpdateServerMods(IEnumerable<ulong> modlist)
        {
            if (_taskBlocker.IsSet(Operations.GameRunning, Operations.SteamDownload)) return;

            SteamWidget.Start("Checking for mod updates...");
            var cts = _taskBlocker.Set(Operations.SteamDownload);
            Task.Run(async () =>
            {
                try
                {
                    await _trebuchet.Steam.UpdateMods(modlist, cts);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        new ErrorModal("Mod update failed", $"{ex.Message + Environment.NewLine}Please check the log for more information.").ShowDialog();
                    });
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _taskBlocker.Release(Operations.SteamDownload);
                    });
                }
            }, cts.Token);
        }

        private void UpdateServers()
        {
            if (_taskBlocker.IsSet(Operations.GameRunning, Operations.SteamDownload)) return;

            SteamWidget.Start("Checking for mod updates...");
            var cts = _taskBlocker.Set(Operations.SteamDownload);

            Task.Run(async () =>
            {
                try
                {
                    await _trebuchet.Steam.UpdateServerInstances(cts);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        new ErrorModal("Server update failed", $"{ex.Message + Environment.NewLine}Please check the log for more information.").ShowDialog();
                    });
                }
                finally
                {
                    _taskBlocker.Release(Operations.SteamDownload);
                }
            }, cts.Token);
        }

        private void UpdateThenCatapultServer(string profile, string modlist, int instance)
        {
            if (_taskBlocker.IsSet(Operations.GameRunning, Operations.SteamDownload)) return;

            if (_trebuchet.Config.AutoUpdateStatus == AutoUpdateStatus.Never)
            {
                _trebuchet.Launcher.CatapultServer(profile, modlist, instance);
                return;
            }

            SteamWidget.Start("Checking for updates...");
            var cts = _taskBlocker.Set(Operations.SteamDownload);

            if (!ModListProfile.TryLoadProfile(_trebuchet.Config, modlist, out ModListProfile? modlistProfile))
            {
                new ErrorModal("Modlist not found", "The modlist you tried to load does not exist.").ShowDialog();
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    await _trebuchet.Steam.UpdateServerInstances(cts);
                    await _trebuchet.Steam.UpdateMods(modlistProfile.GetModIDList(), cts);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        new ErrorModal("Launch Failed", $"Update failed. Please check the log for more information. ({ex.Message})").ShowDialog();
                    });
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _taskBlocker.Release(Operations.SteamDownload);
                        if (instance >= 0)
                            _trebuchet.Launcher.CatapultServer(profile, modlist, instance);
                        else
                            for (int i = 0; i < _trebuchet.Config.ServerInstanceCount; i++)
                                _trebuchet.Launcher.CatapultServer(profile, modlist, i);
                    });
                }
            });
        }

        private void VerifyFiles(IEnumerable<ulong> modlist)
        {
            if (_taskBlocker.IsSet(Operations.GameRunning, Operations.SteamDownload)) return;

            SteamWidget.Start("Verifying server files and mods...");
            var cts = _taskBlocker.Set(Operations.SteamDownload);

            Task.Run(async () =>
            {
                try
                {
                    _trebuchet.Steam.ClearCache();
                    await _trebuchet.Steam.UpdateServerInstances(cts);
                    await _trebuchet.Steam.UpdateMods(modlist, cts);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        new ErrorModal("File verification failed", $"Please check the log for more information. ({ex.Message})").ShowDialog();
                    });
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _taskBlocker.Release(Operations.SteamDownload);
                    });
                }
            });
        }
    }
}