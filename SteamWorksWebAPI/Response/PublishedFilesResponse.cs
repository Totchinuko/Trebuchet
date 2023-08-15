using System.Text.Json.Serialization;

namespace SteamWorksWebAPI
{
    public class PublishedFilesResponse
    {
        public PublishedFile[] PublishedFileDetails { get; set; } = new PublishedFile[0];

        public int Result { get; set; } = 0;

        public int ResultCount { get; set; } = 0;
    }
}