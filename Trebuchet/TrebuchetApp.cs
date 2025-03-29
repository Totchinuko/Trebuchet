using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Trebuchet.Messages;
using Trebuchet.Panels;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils.Modals;

namespace Trebuchet
{
    public sealed class TrebuchetApp : INotifyPropertyChanged, IRecipient<PanelActivateMessage>
    {
        private readonly Launcher _launcher;
        private readonly Steam _steam;
        private Panel _activePanel;
        private List<Panel> _panels;
        private DispatcherTimer _timer;

        public TrebuchetApp(Launcher launcher, Steam steam, SteamWidget steamWidget, IEnumerable<Panel> panels)
        {
            _launcher = launcher;
            _steam = steam;
            _panels = panels.ToList();
            SteamWidget = steamWidget;

            StrongReferenceMessenger.Default.Register(this);

            OrderPanels(_panels);
            _activePanel = BottomPanels.First(x => x.CanExecute(null));
            _activePanel.Active = true;
            
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTimerTick);
            _timer.Start();
        }

        private async void OnTimerTick(object? sender, EventArgs e)
        {
            _timer.Stop();
            await _launcher.Tick();
            foreach (var panel in _panels)
                panel.Tick();
            _timer.Start();
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
        public ObservableCollection<Panel> BottomPanels { get; } = [];

        public SteamWidget SteamWidget { get; }

        public async void OnWindowShow()
        {
            _panels.ForEach(x => x.OnWindowShow());
            await _steam.Connect();
        }

        public void Receive(PanelActivateMessage message)
        {
            ActivePanel = message.panel;
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
                if(panel.BottomPosition)
                    BottomPanels.Add(panel);
                else
                    TopPanels.Add(panel);
            }
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        //todo: improve write access
        // private bool WriteAccessCheck()
        // {
        //     if (Trebuchet.Utils.Utils.ValidateInstallDirectory(_trebuchet.Config.ResolvedInstallPath, out string _) && !Tools.ValidateDirectoryUac(_trebuchet.Config.ResolvedInstallPath))
        //     {
        //         Utils.Utils.RestartProcess(_trebuchet.Config.IsTestLive, true);
        //         return false;
        //     }
        //     if (Trebuchet.Utils.Utils.ValidateGameDirectory(_trebuchet.Config.ClientPath, out string _) && !Tools.ValidateDirectoryUac(_trebuchet.Config.ClientPath))
        //     {
        //         Utils.Utils.RestartProcess(_trebuchet.Config.IsTestLive, true);
        //         return false;
        //     }
        //     return true;
        // }
        
        
        // public async void Receive(UACPromptRequest message)
        // {
        //     QuestionModal modal = new(
        //         App.GetAppText("UACDialog_Title"),
        //         App.GetAppText("UACDialog", message.Directory)
        //         );
        //     await modal.OpenDialogueAsync();
        //
        //     if (modal.Result)
        //     {
        //         Utils.Utils.RestartProcess(_setup.IsTestLive, true);
        //         message.Reply(true);
        //     }
        //     else
        //         message.Reply(false);
        // }
    }
}