using System.Text.Json.Serialization;

namespace SteamWorksWebAPI
{
    public class PublishedFile
    {
        public byte Banned { get; set; } = 0;
        [JsonPropertyName("ban_reason")]
        public string BanReason { get; set; } = string.Empty;
        [JsonPropertyName("consumer_app_id")]
        public uint ConsumerAppId { get; set; } = 0;
        public string Creator { get; set; } = string.Empty;
        [JsonPropertyName("creator_app_id")]
        public uint CreatorAppId { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
        public ulong Favorited { get; set; } = 0;
        public string Filename { get; set; } = string.Empty;
        [JsonPropertyName("file_size")]
        public long FileSize { get; set; } = 0;
        [JsonPropertyName("file_url")]
        public string FileUrl { get; set; } = string.Empty;
        [JsonPropertyName("hcontent_file")]
        public string HcontentFile { get; set; } = string.Empty;
        [JsonPropertyName("hcontent_preview")]
        public string HcontentPreview { get; set; } = string.Empty;
        [JsonPropertyName("lifetime_favorited")]
        public ulong LifetimeFavorited { get; set; } = 0;
        [JsonPropertyName("lifetime_subscriptions")]
        public ulong LifetimeSubscriptions { get; set; } = 0;
        [JsonPropertyName("preview_url")]
        public string PreviewUrl { get; set; } = string.Empty;
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public ulong PublishedFileID { get; set; } = 0;
        public uint Result { get; set; } = 0;
        public ulong Subscriptions { get; set; } = 0;
        public SteamTag[] Tags { get; set; } = new SteamTag[0];
        [JsonPropertyName("time_created")]
        public ulong TimeCreated { get; set; } = 0;
        [JsonPropertyName("time_updated")]
        public ulong TimeUpdated { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
        public ulong Views { get; set; } = 0;
        public byte Visibility { get; set; } = 0;
    }
}