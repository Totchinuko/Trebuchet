using SteamWorksWebAPI.Response;

namespace SteamWorksWebAPI
{
    public class QueryFilesResponse
    {
        public QueriedPublishedFile[] PublishedFileDetails { get; set; } = new QueriedPublishedFile[0];
        public int Total { get; set; } = 0;
    }
}