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
        
        public int AutoUpdateStatus { get; set; } = 1;
        public string ClientPath { get; set; } = string.Empty;
        public bool ManageClient { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [Obsolete]
        public string InstallPath { get; set; } = string.Empty;
        public int MaxDownloads { get; set; } = 8;
        public int MaxServers { get; set; } = 20;
        public int ServerInstanceCount { get; set; }
        public int UpdateCheckInterval { get; set; } = 300;
        public bool VerifyAll { get; set; }
        
        public string SelectedClientModlist { get; set; } = string.Empty;
        public string SelectedClientProfile { get; set; } = string.Empty;
        public string[] SelectedServerModlists { get => _selectedServerModlists; set => _selectedServerModlists = value; }
        public string[] SelectedServerProfiles { get => _selectedServerProfiles; set => _selectedServerProfiles = value; }

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
            return SelectedServerModlists[instance];
        }
    }
}