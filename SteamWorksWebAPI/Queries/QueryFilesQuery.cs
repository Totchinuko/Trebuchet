using System.Text.Json.Serialization;

namespace SteamWorksWebAPI
{
    public class QueryFilesQuery : Query
    {
        public QueryFilesQuery(string ApiKey)
        {
            Key = ApiKey;
        }

        /// <summary>
        /// App that consumes the files
        /// </summary>
        public uint AppId { get; set; } = 0;

        /// <summary>
        /// Allow stale data to be returned for the specified number of seconds.
        /// </summary>
        [JsonPropertyName("cache_max_age_seconds")]
        public uint CacheMaxAgeSeconds { get; set; } = 0;

        /// <summary>
        /// Find all items that reference the given item.
        /// </summary>
        [JsonPropertyName("child_publishedfileid")]
        public ulong ChildPublishedFileID { get; set; } = 0;

        /// <summary>
        /// App that created the files
        /// </summary>
        [JsonPropertyName("creator_appid")]
        public uint CreatorAppID { get; set; } = 0;

        /// <summary>
        /// Cursor to paginate through the results (set to '*' for the first request).
        /// Prefer this over using the page parameter, as it will allow you to do deep pagination.  When used, the page parameter will be ignored.
        /// </summary>
        public string Cursor { get; set; } = string.Empty;

        /// <summary>
        /// If query_type is k_PublishedFileQueryType_RankedByTrend, then this is the number of days to get votes for [1,7].
        /// </summary>
        public uint Days { get; set; } = 0;

        /// <summary>
        /// (Optional) Tags that must NOT be present on a published file to satisfy the query.
        /// </summary>
        public string ExcludeTags { get; set; } = string.Empty;

        public PublishedFileType FileType { get; set; } = PublishedFileType.Items;

        /// <summary>
        /// (Optional) If true, only return the published file ids of files that satisfy this query.
        /// </summary>
        [JsonPropertyName("ids_only")]
        public bool IdsOnly { get; set; } = false;

        /// <summary>
        /// If query_type is k_PublishedFileQueryType_RankedByTrend, then limit result set just to items that have votes within the day range given
        /// </summary>
        [JsonPropertyName("include_recent_votes_only")]
        public bool IncludeRecentVotesOnly { get; set; } = false;

        /// <summary>
        /// Access key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// If true, then items must have all the tags specified, otherwise they must have at least one of the tags.
        /// </summary>
        [JsonPropertyName("match_all_tags")]
        public bool MatchAllTags { get; set; } = false;

        /// <summary>
        /// (Optional) The number of results, per page to return.
        /// </summary>
        public uint NumPerPage { get; set; } = 0;

        /// <summary>
        /// Flags that must not be set on any returned items
        /// </summary>
        [JsonPropertyName("omitted_flags")]
        public string OmittedFlags { get; set; } = string.Empty;

        /// <summary>
        /// Current page
        /// </summary>
        public uint Page { get; set; } = 0;

        [JsonPropertyName("query_type")]
        public PublishedFileQueryType QueryType { get; set; } = PublishedFileQueryType.RankedByVote;

        /// <summary>
        /// Required flags that must be set on any returned items
        /// </summary>
        [JsonPropertyName("required_flags")]
        public string RequiredFlags { get; set; } = string.Empty;

        /// <summary>
        /// Tags to match on. See match_all_tags parameter below
        /// </summary>
        public string RequiredTags { get; set; } = string.Empty;

        /// <summary>
        /// Return child item ids in the file details
        /// </summary>
        [JsonPropertyName("return_children")]
        public bool ReturnChildren { get; set; } = false;

        /// <summary>
        /// By default, if none of the other 'return_*' fields are set, only some voting details are returned. Set this to true to return the default set of details.
        /// </summary>
        [JsonPropertyName("return_details")]
        public bool ReturnDetails { get; set; } = false;

        /// <summary>
        /// Return pricing information, if applicable
        /// </summary>
        [JsonPropertyName("return_for_sale_data")]
        public bool ReturnForSaleData { get; set; } = false;

        /// <summary>
        /// Return key-value tags in the file details
        /// </summary>
        [JsonPropertyName("return_kv_tags")]
        public bool ReturnKVTags { get; set; } = false;

        /// <summary>
        /// Populate the metadata
        /// </summary>
        [JsonPropertyName("return_metadata")]
        public bool ReturnMetadata { get; set; } = false;

        /// <summary>
        /// Return playtime stats for the specified number of days before today.
        /// </summary>
        [JsonPropertyName("return_playtime_stats")]
        public uint ReturnPlaytimeStats { get; set; } = 0;

        /// <summary>
        /// Return preview image and video details in the file details
        /// </summary>
        [JsonPropertyName("return_previews")]
        public bool ReturnPreviews { get; set; } = false;

        /// <summary>
        /// Return the data for the specified revision.
        /// </summary>
        [JsonPropertyName("return_reactions")]
        public bool ReturnReactions { get; set; } = false;

        /// <summary>
        /// Populate the short_description field instead of file_description
        /// </summary>
        [JsonPropertyName("return_short_description")]
        public bool ReturnShortDescription { get; set; } = false;

        /// <summary>
        /// Return tags in the file details
        /// </summary>
        [JsonPropertyName("return_tags")]
        public bool ReturnTags { get; set; } = false;

        /// <summary>
        /// Return vote data
        /// </summary>
        [JsonPropertyName("return_vote_data")]
        public bool ReturnVoteData { get; set; } = false;

        /// <summary>
        /// Text to match in the item's title or description
        /// </summary>
        [JsonPropertyName("search_text")]
        public string SearchText { get; set; } = string.Empty;

        /// <summary>
        /// Strips BBCode from descriptions.
        /// </summary>
        [JsonPropertyName("strip_description_bbcode")]
        public bool StripDescriptionBBcode { get; set; } = true;

        /// <summary>
        /// (Optional) If true, only return the total number of files that satisfy this query.
        /// </summary>
        public bool TotalOnly { get; set; } = false;
    }
}