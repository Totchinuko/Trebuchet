using Goog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoogLib
{
    public class SteamCollectionDetailsResult
    {
        public int result = 0;
        [JsonPropertyName("resultcount")]
        public int resultCount = 0;
        [JsonPropertyName("collectiondetails")]
        public List<SteamCollectionDetails>? collectionDetails = null;
    }
}
