using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using Trebuchet.Messages;
using Trebuchet.Panels;
using Trebuchet.Services;
using Trebuchet.Utils;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils.Modals;

namespace Trebuchet
{
    public sealed class TrebuchetApp : INotifyPropertyChanged,
        IRecipient<CloseProcessMessage>,
        IRecipient<PanelActivateMessage>,
        IRecipient<ServerConsoleRequest>,
        IRecipient<ProcessServerDetailsRequest>,
        IRecipient<UACPromptRequest>
    {
        private readonly Launcher _launcher;
        private readonly AppSetup _setup;
        private readonly Steam _steam;
        private Panel _activePanel;
        private List<Panel> _panels;


        public TrebuchetApp(Launcher launcher, AppSetup setup, Steam steam, IEnumerable<Panel> panels)
        {
            _launcher = launcher;
            _setup = setup;
            _steam = steam;
            _panels = panels.ToList();

            StrongReferenceMessenger.Default.Register<CloseProcessMessage>(this);
            StrongReferenceMessenger.Default.Register<KillProcessMessage>(this);
            StrongReferenceMessenger.Default.Register<ShutdownProcessMessage>(this);
            StrongReferenceMessenger.Default.Register<ServerConsoleRequest>(this);
            StrongReferenceMessenger.Default.Register<PanelActivateMessage>(this);
            StrongReferenceMessenger.Default.Register<ProcessServerDetailsRequest>(this);
            StrongReferenceMessenger.Default.Register<UACPromptRequest>(this);

            OrderPanels(_panels);
            _activePanel = BottomPanels.First(x => x.CanExecute(null));
            if (!WriteAccessCheck()) return;
            _activePanel.Active = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static string AppTitle => $"Tot ! Trebuchet {TrebuchetUtils.Utils.GetFileVersion()}";
        public Panel ActivePanel
        {
            get => _activePanel;
            set
            {
                if(_activePanel == value) return;
                _activePanel.Active = false;
                _activePanel = value;
                _activePanel.Active = true;
                _activePanel.PanelDisplayed();
                OnPropertyChanged(nameof(ActivePanel));
            }
        }

        public ObservableCollection<Panel> TopPanels { get; } = [];
        public ObservableCollection<Panel> ClientPanels { get; } = [];
        public ObservableCollection<Panel> ServerPanels { get; } = [];
        public ObservableCollection<Panel> BottomPanels { get; } = [];

        public SteamWidget SteamWidget { get; } = new SteamWidget();

        public async void OnWindowShow()
        {
            _panels.ForEach(x => x.OnWindowShow());
            await _steam.Connect();
        }

        public void Receive(PanelActivateMessage message)
        {
            ActivePanel = message.panel;
        }

        public void Receive(ServerConsoleRequest message)
        {
            message.Reply(_launcher.GetServerConsole(message.instance));
        }

        public void Receive(ProcessServerDetailsRequest message)
        {
            message.Reply(_launcher.GetServerProcesses().ToList());
        }

        public async void Receive(UACPromptRequest message)
        {
            QuestionModal modal = new(
                App.GetAppText("UACDialog_Title"),
                App.GetAppText("UACDialog", message.Directory)
                );
            await modal.OpenDialogueAsync();

            if (modal.Result)
            {
                Utils.Utils.RestartProcess(_setup.IsTestLive, true);
                message.Reply(true);
            }
            else
                message.Reply(false);
        }
        
        async void IRecipient<CloseProcessMessage>.Receive(CloseProcessMessage message)
        {
            if (message.instance >= 0)
                if (message is ShutdownProcessMessage)
                    throw new NotImplementedException();
                else if (message is KillProcessMessage)
                    await _launcher.KillServer(message.instance);
                else
                    await _launcher.CloseServer(message.instance);
            else
                await _launcher.KillClient();
        }

        internal void OnAppClose()
        {
            _launcher.Dispose();
            _steam.Disconnect();
            Task.Run(() =>
            {
                while (_steam.IsConnected)
                    Task.Delay(100);
            }).Wait();
        }

        private void OrderPanels(List<Panel> panels)
        {
            foreach (var panel in panels)
            {
                switch (panel.Position)
                {
                    case PanelPosition.Top:
                        TopPanels.Add(panel);
                        break;
                    case PanelPosition.Bottom:
                        BottomPanels.Add(panel);
                        break;
                    case PanelPosition.Client:
                        ClientPanels.Add(panel);
                        break;
                    case PanelPosition.Server:
                        ServerPanels.Add(panel);
                        break;
                }
            }
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void UpdateThenCatapultServer(IEnumerable<(string profile, string modlist, int instance)> instances)
        {
            if (!TrebuchetUtils.GuiExtensions.Assert(!_taskBlocker.IsSet(Operations.SteamDownload), "Trebuchet is busy.")) return;

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

        private bool WriteAccessCheck()
        {
            if (Trebuchet.Utils.Utils.ValidateInstallDirectory(_trebuchet.Config.ResolvedInstallPath, out string _) && !Tools.ValidateDirectoryUac(_trebuchet.Config.ResolvedInstallPath))
            {
                Utils.Utils.RestartProcess(_trebuchet.Config.IsTestLive, true);
                return false;
            }
            if (Trebuchet.Utils.Utils.ValidateGameDirectory(_trebuchet.Config.ClientPath, out string _) && !Tools.ValidateDirectoryUac(_trebuchet.Config.ClientPath))
            {
                Utils.Utils.RestartProcess(_trebuchet.Config.IsTestLive, true);
                return false;
            }
            return true;
        }
    }
}