using Goog;
using GoogLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public class GoogApp : INotifyPropertyChanged
    {
        private Panel? _activePanel;
        private JsonSerializerOptions _options;

        public GoogApp(Config config, UIConfig uiConfig)
        {
            Config = config;
            UiConfig = uiConfig;
            Steam = new SteamSession(Config);
            SteamWidget = new SteamWidget(Steam);
            Trebuchet = new Trebuchet(config);
            Trebuchet.DispatcherRequest += OnTrebuchetRequestDispatcher;
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new MenuElementFactory(Config, UiConfig, Steam, Trebuchet, SteamWidget)
            };

            var menuConfig = GuiExtensions.GetEmbededTextFile("GoogGUI.GoogApp.Menu.json");
            Menu = JsonSerializer.Deserialize<Menu>(menuConfig, _options) ?? throw new Exception("Could not deserialize the menu.");
            Panels = Menu.GetPanels().ToList();

            Steam.Connect();
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
                OnPropertyChanged("ActivePanel");
            }
        }

        public Config Config { get; }

        public Menu Menu { get; set; } = new Menu();

        public List<Panel> Panels { get; set; } = new List<Panel>();

        public SteamSession Steam { get; }

        public Trebuchet Trebuchet { get; }

        public UIConfig UiConfig { get; }

        public SteamWidget SteamWidget { get; }

        public void BaseChecks()
        {
            if (!string.IsNullOrEmpty(Config.InstallPath) && !Tools.CanWriteHere(Config.InstallPath))
                new ErrorModal("Install Folder Error", "Cannot access the install folder", false).ShowDialog();

            if (string.IsNullOrEmpty(Config.InstallPath))
                new MessageModal("Install Folder", "In order to use Goog, please configure a folder to install your mods and profiles").ShowDialog();

            if (!Config.IsInstallPathValid)
                ActivePanel = (Panel)Menu.Bottom.Where(x => x.GetType() == typeof(Settings)).First();
            else
                ActivePanel = (Panel)Menu.Bottom.Where(x => x.GetType() == typeof(Dashboard)).First();
        }

        public T GetPanel<T>() where T : Panel
        {
            T panel = (T)Panels.Where(p => p.GetType() == typeof(T)).First();
            if (panel == null) throw new Exception("Unknown Panel.");
            return panel;
        }

        public virtual void OnAppClose()
        {
            Steam.Disconnect();
            Task.Run(() =>
            {
                while(Steam.Client.IsConnected)
                    Task.Delay(100);
            }).Wait();
        }

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void OnAppConfigurationChanged(object? sender, EventArgs e)
        {
            Panels.ForEach(p => p.RefreshPanel());
        }

        private void OnTrebuchetRequestDispatcher(object? sender, Action e)
        {
            Application.Current.Dispatcher.Invoke(e);
        }
    }
}