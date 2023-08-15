using System.Text.Json.Serialization;

namespace SteamWorksWebAPI
{
    public class PublishedFilesResponse
    {
        public List<PublishedFile> PublishedFileDetails { get; set; } = new List<PublishedFile>();

        public int Result { get; set; } = 0;

        public int ResultCount { get; set; } = 0;
    }
}