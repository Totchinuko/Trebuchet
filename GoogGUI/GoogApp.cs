using Goog;
using GoogGUI.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace GoogGUI
{
    public class GoogApp : INotifyPropertyChanged
    {
        private List<Panel> _bottomTabs = new List<Panel>();
        private Config _config;
        private UIConfig _uiConfig;
        private Panel? _panel;
        private List<Panel> _topTabs = new List<Panel>();

        public GoogApp(Config config, UIConfig uiConfig)
        {
            _config = config;
            _uiConfig = uiConfig;

            IEnumerable<Type> types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => x.GetCustomAttributes<PanelAttribute>().Any())
                .OrderBy(type => type.GetCustomAttribute<PanelAttribute>()?.Sort ?? 0);

            foreach (Type t in types)
            {
                Panel? panel = (Panel?)Activator.CreateInstance(t, _config, _uiConfig) ?? throw new Exception("Panel attribute must be placed on Panel classes.");
                if (panel == null) continue;
                if (t.GetCustomAttribute<PanelAttribute>()?.Bottom ?? false)
                    _bottomTabs.Add(panel);
                else
                    _topTabs.Add(panel);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public List<Panel> BottomTabs { get => _bottomTabs; }

        public Panel? Panel
        {
            get => _panel;
            set
            {
                if (_panel != null)
                    _panel.Active = false;
                _panel = value;
                if (_panel != null)
                    _panel.Active = true;
                OnPropertyChanged("Panel");
            }
        }

        public List<Panel> TopTabs { get => _topTabs; }

        public void BaseChecks()
        {
            if (!string.IsNullOrEmpty(_config.InstallPath) && !Tools.CanWriteHere(_config.InstallPath))
                new ErrorModal("Install Folder Error", "Cannot access the install folder", false).ShowDialog();

            if (string.IsNullOrEmpty(_config.InstallPath))
                new MessageModal("Install Folder", "In order to use Goog, please configure a folder to install your mods and profiles").ShowDialog();

            if (!_config.IsInstallPathValid)
                Panel = _bottomTabs.Where(x => x.GetType() == typeof(Settings)).FirstOrDefault();
            else
                Panel = _bottomTabs.Where(x => x.GetType() == typeof(Dashboard)).FirstOrDefault();
        }

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}