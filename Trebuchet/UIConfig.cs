using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Trebuchet
{
    public sealed class UIConfig : ConfigFile<UIConfig>
    {
        private string[] _dashboardServerModlist = new string[0];
        private string[] _dashboardServerProfiles = new string[0];

        public bool AutoRefreshModlist { get; set; } = true;

        public string CurrentClientProfile { get; set; } = string.Empty;

        public string CurrentModlistProfile { get; set; } = string.Empty;

        public string CurrentServerProfile { get; set; } = string.Empty;

        public string DashboardClientModlist { get; set; } = string.Empty;

        public string DashboardClientProfile { get; set; } = string.Empty;

        public string[] DashboardServerModlist { get => _dashboardServerModlist; set => _dashboardServerModlist = value; }

        public string[] DashboardServerProfiles { get => _dashboardServerProfiles; set => _dashboardServerProfiles = value; }

        public bool DisplayProcessPerformance { get; set; } = true;

        public bool DisplayWarningOnKill { get; set; } = true;

        public bool UseHardwareAcceleration { get; set; } = true;

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
            if (DashboardServerModlist.Length <= instance) return;

            modlist = DashboardServerModlist[instance];
            profile = DashboardServerProfiles[instance];
        }

        public void SetInstanceParameters(int instance, string modlist, string profile)
        {
            if (DashboardServerModlist.Length <= instance)
                Array.Resize(ref _dashboardServerModlist, instance + 1);
            DashboardServerModlist[instance] = modlist;

            if (DashboardServerProfiles.Length <= instance)
                Array.Resize(ref _dashboardServerProfiles, instance + 1);
            DashboardServerProfiles[instance] = profile;
        }
    }
}