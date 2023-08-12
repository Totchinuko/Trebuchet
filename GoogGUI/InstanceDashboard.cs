using Goog;
using GoogLib;
using System.ComponentModel;

namespace GoogGUI
{
    public class InstanceDashboard : INotifyPropertyChanged
    {
        private Config _config;
        private int _instance;
        private string _selectedModlist = string.Empty;
        private string _selectedProfile = string.Empty;
        private Trebuchet _trebuchet;
        private UIConfig _uiConfig;

        public InstanceDashboard(Config config, UIConfig uiConfig, Trebuchet trebuchet, int instance)
        {
            _config = config;
            _instance = instance;
            _trebuchet = trebuchet;
            _uiConfig = uiConfig;

            _uiConfig.GetInstanceParameters(_instance, out _selectedModlist, out _selectedProfile);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SelectedModlist { get => _selectedModlist; set => _selectedModlist = value; }

        public string SelectedProfile { get => _selectedProfile; set => _selectedProfile = value; }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}