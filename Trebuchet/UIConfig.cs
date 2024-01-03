using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Trebuchet
{
    public sealed class UIConfig : ConfigFile<UIConfig>
    {
        private string _currentClientProfile = string.Empty;
        private string _currentModlistProfile = string.Empty;
        private string _currentServerProfile = string.Empty;
        private string _dashboardClientModlist = string.Empty;
        private string _dashboardClientProfile = string.Empty;
        private string[] _dashboardServerModlist = new string[0];
        private string[] _dashboardServerProfiles = new string[0];
        private bool _displayWarningOnKill = true;
        private bool _useHardwareAcceleration = true;

        public string CurrentClientProfile { get => _currentClientProfile; set => _currentClientProfile = value; }

        public string CurrentModlistProfile { get => _currentModlistProfile; set => _currentModlistProfile = value; }

        public string CurrentServerProfile { get => _currentServerProfile; set => _currentServerProfile = value; }

        public string DashboardClientModlist { get => _dashboardClientModlist; set => _dashboardClientModlist = value; }

        public string DashboardClientProfile { get => _dashboardClientProfile; set => _dashboardClientProfile = value; }

        public string[] DashboardServerModlist { get => _dashboardServerModlist; set => _dashboardServerModlist = value; }

        public string[] DashboardServerProfiles { get => _dashboardServerProfiles; set => _dashboardServerProfiles = value; }

        public bool DisplayProcessPerformance { get; set; } = true;

        public bool DisplayWarningOnKill { get => _displayWarningOnKill; set => _displayWarningOnKill = value; }

        public bool UseHardwareAcceleration { get => _useHardwareAcceleration; set => _useHardwareAcceleration = value; }

        public static string GetPath(bool testlive)
        {
            string? ConfigPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            if (string.IsNullOrEmpty(ConfigPath))
                throw new Exception("Path to assembly is invalid.");
            ConfigPath = Path.Combine(ConfigPath, $"{(testlive ? Config.FolderTestLive : Config.FolderLive)}.UIConfig.json");
            return ConfigPath;
        }

        public void GetInstanceParameters(int instance, out string modlist, out string profile)
        {
            modlist = string.Empty;
            profile = string.Empty;
            if (_dashboardServerModlist.Length <= instance) return;

            modlist = _dashboardServerModlist[instance];
            profile = _dashboardServerProfiles[instance];
        }

        public void SetInstanceParameters(int instance, string modlist, string profile)
        {
            if (_dashboardServerModlist.Length <= instance)
                Array.Resize(ref _dashboardServerModlist, instance + 1);
            _dashboardServerModlist[instance] = modlist;

            if (_dashboardServerProfiles.Length <= instance)
                Array.Resize(ref _dashboardServerProfiles, instance + 1);
            _dashboardServerProfiles[instance] = profile;
        }
    }
}