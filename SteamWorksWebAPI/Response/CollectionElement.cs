using SteamWorksWebAPI;
using System.Text.Json.Serialization;

namespace SteamWorksWebAPI
{
    public class CollectionElement
    {
        public PublishedFileType FileType { get; set; } = PublishedFileType.Items;

        public string PublishedFileId { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;
    }
}