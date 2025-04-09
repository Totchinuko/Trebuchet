using System.Text.Json.Serialization;
using TrebuchetUtils;

namespace TrebuchetLib
{
    public static class AutoUpdateStatus
    {
        public const int CheckForUpdates = 2;
        public const int Never = 0;
        public const int OnlyOnStart = 1;
    }

    public sealed class Config : ConfigFile<Config>
    {
        private string[] _selectedServerModlists = [];
        private string[] _selectedServerProfiles = [];
        
        public int AutoUpdateStatus { get; set; } = AutoUpdateStatusDefault;
        public string ClientPath { get; set; } = ClientPathDefault;
        public bool ManageClient { get; set; } = ManageClientDefault;
        public int MaxDownloads { get; set; } = MaxDownloadsDefault;
        public int MaxServers { get; set; } = MaxServersDefault;
        public int ServerInstanceCount { get; set; } = ServerInstanceCountDefault;
        public int UpdateCheckInterval { get; set; } = UpdateCheckIntervalDefault;
        public bool VerifyAll { get; set; } = VerifyAllDefault;
        public string SelectedClientModlist { get; set; } = string.Empty;
        public string SelectedClientProfile { get; set; } = string.Empty;
        public string[] SelectedServerModlists { get => _selectedServerModlists; set => _selectedServerModlists = value; }
        public string[] SelectedServerProfiles { get => _selectedServerProfiles; set => _selectedServerProfiles = value; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [Obsolete]
        public string InstallPath { get; set; } = string.Empty;
        

        public static string GetDefaultInstallPath()
        {
            return typeof(Config).GetStandardFolder(Environment.SpecialFolder.MyDocuments).FullName;
        }
        
        public void SetInstanceParameters(int instance, string modlist, string profile)
        {
            SetInstanceModlist(instance, modlist);
            SetInstanceProfile(instance, profile);
        }
        
        public void SetInstanceModlist(int instance, string modlist)
        {
            if (SelectedServerModlists.Length <= instance)
                Array.Resize(ref _selectedServerModlists, instance + 1);
            SelectedServerModlists[instance] = modlist;
        }

        public void SetInstanceProfile(int instance, string profile)
        {
            if (SelectedServerProfiles.Length <= instance)
                Array.Resize(ref _selectedServerProfiles, instance + 1);
            SelectedServerProfiles[instance] = profile;
        }
        
        public void GetInstanceParameters(int instance, out string modlist, out string profile)
        {
            modlist = GetInstanceModlist(instance);
            profile = GetInstanceProfile(instance);
        }

        public string GetInstanceModlist(int instance)
        {
            if (SelectedServerModlists.Length <= instance) return string.Empty;
            return SelectedServerModlists[instance];
        }

        public string GetInstanceProfile(int instance)
        {
            if (SelectedServerProfiles.Length <= instance) return string.Empty;
            return SelectedServerProfiles[instance];
        }
        
        public static readonly int AutoUpdateStatusDefault = 1;
        public static readonly string ClientPathDefault = string.Empty;
        public static readonly bool ManageClientDefault = false;
        public static readonly int MaxDownloadsDefault = 8;
        public static readonly int MaxServersDefault = 20;
        public static readonly int ServerInstanceCountDefault = 0;
        public static readonly int UpdateCheckIntervalDefault = 300;
        public static readonly bool VerifyAllDefault = false;
    }
}