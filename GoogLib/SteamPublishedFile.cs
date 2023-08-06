using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Goog
{
    public class SteamPublishedFile
    {
        public string publishedFileID = "";
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int result = 0;
        public string creator = "";
        [JsonPropertyName("creator_app_id")]
        public int creatorAppId = 0;
        [JsonPropertyName("consumer_app_id")]
        public int consumerAppId = 0;
        public string filename = "";
        [JsonPropertyName("file_size")]
        public long fileSize = 0;
        [JsonPropertyName("file_url")]
        public string fileUrl = "";
        [JsonPropertyName("hcontent_file")]
        public string hcontentFile = "";
        [JsonPropertyName("preview_url")]
        public string previewUrl = "";
        [JsonPropertyName("hcontent_preview")]
        public string hcontentPreview = "";
        public string title = "";
        public string description = "";
        [JsonPropertyName("time_created")]
        public long timeCreated = 0;
        [JsonPropertyName("time_updated")]
        public long timeUpdated = 0;
        public byte visibility = 0;
        public byte banned = 0;
        [JsonPropertyName("ban_reason")]
        public string banReason = "";
        public long subscriptions = 0;
        public long favorited = 0;
        [JsonPropertyName("lifetime_subscriptions")]
        public long lifetimeSubscriptions = 0;
        [JsonPropertyName("lifetime_favorited")]
        public long lifetimeFavorited = 0;
        public long views = 0;
        public string[] tags = new string[0];
    }
}
