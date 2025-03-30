using System;
using System.IO;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet
{
    public sealed class UIConfig : ConfigFile<UIConfig>
    {
        private string[] _dashboardServerModlist = [];
        private string[] _dashboardServerProfiles = [];

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
            SetInstanceModlist(instance, modlist);
            SetInstanceProfile(instance, profile);
        }

        public void SetInstanceModlist(int instance, string modlist)
        {
            if (DashboardServerModlist.Length <= instance)
                Array.Resize(ref _dashboardServerModlist, instance + 1);
            DashboardServerModlist[instance] = modlist;
        }

        public void SetInstanceProfile(int instance, string profile)
        {
            if (DashboardServerProfiles.Length <= instance)
                Array.Resize(ref _dashboardServerProfiles, instance + 1);
            DashboardServerProfiles[instance] = profile;
        }
    }
}