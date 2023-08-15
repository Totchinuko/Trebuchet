using System.Text.Json.Serialization;

namespace SteamWorksWebAPI
{
    public class GetDetailsQuery : Query
    {
        public uint AppID { get; set; } = 0;

        /// <summary>
        /// If true, return preview information in the returned details.
        /// </summary>
        public bool IncludeAdditionalPreviews { get; set; } = false;

        /// <summary>
        /// If true, return children in the returned details.
        /// </summary>
        public bool IncludeChildren { get; set; } = false;

        /// <summary>
        /// If true, return pricing data, if applicable.
        /// </summary>
        public bool IncludeForSaleData { get; set; } = false;

        /// <summary>
        /// If true, return key value tags in the returned details.
        /// </summary>
        public bool IncludeKVTags { get; set; } = false;

        /// <summary>
        /// If true, populate the metadata field.
        /// </summary>
        public bool IncludeMetaData { get; set; } = false;

        /// <summary>
        /// If true, then reactions to items will be returned.
        /// </summary>
        public bool IncludeReactions { get; set; } = false;

        /// <summary>
        /// If true, return tag information in the returned details.
        /// </summary>
        public bool IncludeTags { get; set; } = false;

        /// <summary>
        /// If true, return vote data in the returned details.
        /// </summary>
        public bool IncludeVotes { get; set; } = false;

        /// <summary>
        /// Access key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// published file id to look up
        /// </summary>
        public List<ulong> PublishedFileIds { get; set; } = new List<ulong>();

        /// <summary>
        /// Return playtime stats for the specified number of days before today.
        /// </summary>
        [JsonPropertyName("return_playtime_stats")]
        public uint ReturnPlaytimeStats { get; set; } = 0;

        /// <summary>
        /// Strips BBCode from descriptions.
        /// </summary>
        [JsonPropertyName("strip_description_bbcode")]
        public bool StripDescriptionBBcode { get; set; } = false;
    }
}