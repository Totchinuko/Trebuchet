using System.Text.Json.Serialization;

namespace SteamWorksWebAPI
{
    public class CollectionDetailsResponse
    {
        public List<CollectionDetails> CollectionDetails { get; set; } = new List<CollectionDetails>();
        public int Result { get; set; } = 0;
        public int ResultCount { get; set; } = 0;
    }
}