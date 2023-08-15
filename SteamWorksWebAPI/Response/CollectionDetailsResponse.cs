using System.Text.Json.Serialization;

namespace SteamWorksWebAPI
{
    public class CollectionDetailsResponse
    {
        public CollectionDetails[] CollectionDetails { get; set; } = new CollectionDetails[0];
        public int Result { get; set; } = 0;
        public int ResultCount { get; set; } = 0;
    }
}