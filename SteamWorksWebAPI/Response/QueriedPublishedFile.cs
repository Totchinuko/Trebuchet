using System.Text.Json.Serialization;

namespace SteamWorksWebAPI.Response
{
    public class QueriedPublishedFile
    {
        [JsonPropertyName("app_name")]
        public string AppName { get; set; } = string.Empty;
        public bool Banned { get; set; } = false;
        public string Banner { get; set; } = string.Empty;
        [JsonPropertyName("ban_reason")]
        public string BanReason { get; set; } = string.Empty;
        [JsonPropertyName("ban_text_check_result")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int BanTextCheckResult { get; set; } = 0;
        [JsonPropertyName("can_be_deleted")]
        public bool CanBeDeleted { get; set; } = false;
        [JsonPropertyName("can_subscribe")]
        public bool CanSubscribe { get; set; } = false;
        [JsonPropertyName("consumer_appid")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public uint ConsumerAppID { get; set; } = 0;
        [JsonPropertyName("consumer_shortcutid")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int ConsumerShortcutID { get; set; } = 0;
        public string Creator { get; set; } = string.Empty;
        [JsonPropertyName("creator_appid")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public uint CreatorAppID { get; set; } = 0;
        public uint Favorited { get; set; } = 0;
        [JsonPropertyName("file_description")]
        public string FileDescription { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        [JsonPropertyName("file_size")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long FileSize { get; set; } = 0;
        [JsonPropertyName("file_type")]
        public int FileType { get; set; } = 0;
        public int Flags { get; set; } = 0;
        public uint Followers { get; set; } = 0;
        [JsonPropertyName("hcontent_file")]
        public string HContentFile { get; set; } = string.Empty;
        [JsonPropertyName("hcontent_preview")]
        public string HContentPreview { get; set; } = string.Empty;
        public int Language { get; set; } = 0;
        [JsonPropertyName("lifetime_favorited")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public uint LifetimeFavorited { get; set; } = 0;
        [JsonPropertyName("lifetime_followers")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public uint LifetimeFollowers { get; set; } = 0;
        [JsonPropertyName("lifetime_playtime")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public uint LifetimePlaytime { get; set; } = 0;
        [JsonPropertyName("lifetime_playtime_sessions")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public uint LifetimePlaytimeSessions { get; set; } = 0;
        [JsonPropertyName("lifetime_subscriptions")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public uint LifetimeSubscriptions { get; set; } = 0;
        [JsonPropertyName("maybe_inappropriate_sex")]
        public bool MaybeInappropriateSex { get; set; } = false;
        [JsonPropertyName("maybe_inappropriate_violence")]
        public bool MaybeInappropriateViolence { get; set; } = false;
        [JsonPropertyName("num_children")]
        public uint NumChildren { get; set; } = 0;
        [JsonPropertyName("num_comments_public")]
        public uint NumCommentsPublic { get; set; } = 0;
        [JsonPropertyName("num_reports")]
        public uint NumReports { get; set; } = 0;
        [JsonPropertyName("preview_file_size")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public ulong PreviewFileSize { get; set; } = 0;
        [JsonPropertyName("preview_url")]
        public string PreviewUrl { get; set; } = string.Empty;
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public ulong PublishedFileID { get; set; } = 0;
        public int Result { get; set; } = 0;
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Revision { get; set; } = 0;
        [JsonPropertyName("revision_change_number")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int RevisionChangeNumber { get; set; } = 0;
        [JsonPropertyName("short_description")]
        public string ShortDescription { get; set; } = string.Empty;
        [JsonPropertyName("show_subscribe_all")]
        public bool ShowSubscribeAll { get; set; } = false;
        public uint Subscriptions { get; set; } = 0;
        [JsonPropertyName("time_created")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public ulong TimeCreated { get; set; } = 0;
        [JsonPropertyName("time_updated")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public ulong TimeUpdated { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public uint Views { get; set; } = 0;
        public int Visibility { get; set; } = 0;
        [JsonPropertyName("vote_data")]
        public VoteData VoteData { get; set; } = new VoteData();
        [JsonPropertyName("workshop_accepted")]
        public bool WorkshopAccepted { get; set; } = false;
        [JsonPropertyName("workshop_file")]
        public bool WorkshopFile { get; set; } = false;
    }
}