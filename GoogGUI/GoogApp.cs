using Goog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace GoogGUI
{
    public class GoogApp : INotifyPropertyChanged
    {
        private Panel? _activePanel;
        private List<object> _bottomTabs = new List<object>();
        private Config _config;
        private List<Panel> _panels = new List<Panel>();
        private List<object> _topTabs = new List<object>();
        private UIConfig _uiConfig;

        public GoogApp(Config config, UIConfig uiConfig)
        {
            _config = config;
            _uiConfig = uiConfig;

            BuildTabs();
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

        public List<object> BottomTabs { get => _bottomTabs; }

        public List<object> TopTabs { get => _topTabs; }

        public void BaseChecks()
        {
            if (!string.IsNullOrEmpty(_config.InstallPath) && !Tools.CanWriteHere(_config.InstallPath))
                new ErrorModal("Install Folder Error", "Cannot access the install folder", false).ShowDialog();

            if (string.IsNullOrEmpty(_config.InstallPath))
                new MessageModal("Install Folder", "In order to use Goog, please configure a folder to install your mods and profiles").ShowDialog();

            if (!_config.IsInstallPathValid)
                ActivePanel = (Panel)_bottomTabs.Where(x => x.GetType() == typeof(Settings)).First();
            else
                ActivePanel = (Panel)_bottomTabs.Where(x => x.GetType() == typeof(Dashboard)).First();
        }

        public T GetPanel<T>() where T : Panel
        {
            T panel = (T)_panels.Where(p => p.GetType() == typeof(T)).First();
            if (panel == null) throw new Exception("Unknown Panel.");
            return panel;
        }

        protected virtual void BuildTabs()
        {
            IEnumerable<Type> types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => x.GetCustomAttributes<PanelAttribute>().Any())
                .OrderBy(type => !type.GetCustomAttribute<PanelAttribute>()?.Bottom)
                .ThenBy(type => type.GetCustomAttribute<PanelAttribute>()?.Group)
                .ThenBy(type => type.GetCustomAttribute<PanelAttribute>()?.Sort ?? 0);

            string group = string.Empty;
            foreach (Type t in types)
            {
                Panel? panel = (Panel?)Activator.CreateInstance(t, _config, _uiConfig) ?? throw new Exception("Panel attribute must be placed on Panel classes.");
                if (panel == null) continue;
                PanelAttribute? attr = t.GetCustomAttribute<PanelAttribute>();
                if (attr == null) continue;

                panel.AppConfigurationChanged += OnAppConfigurationChanged;

                if (attr.Group != group)
                {
                    group = attr.Group;
                    if (!string.IsNullOrEmpty(group))
                    {
                        if (attr.Bottom)
                            _bottomTabs.Add(group);
                        else
                            _topTabs.Add(group);
                    }
                }

                if (attr.Bottom)
                    _bottomTabs.Add(panel);
                else
                    _topTabs.Add(panel);
                _panels.Add(panel);
            }
        }

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void OnAppConfigurationChanged(object? sender, EventArgs e)
        {
            _panels.ForEach(p => p.RefreshPanel());
        }
    }
}