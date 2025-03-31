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
        public int AutoUpdateStatus { get; set; } = 1;

        public string ClientPath { get; set; } = string.Empty;
        public bool ManageClient { get; set; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [Obsolete]
        public string InstallPath { get; set; } = string.Empty;

        public int MaxDownloads { get; set; } = 8;

        public int MaxServers { get; set; } = 20;

        public int ServerInstanceCount { get; set; } = 0;

        public int UpdateCheckInterval { get; set; } = 300;

        public bool VerifyAll { get; set; } = false;

        public static string GetDefaultInstallPath()
        {
            return typeof(Config).GetStandardFolder(Environment.SpecialFolder.MyDocuments).FullName;
        }
    }
}