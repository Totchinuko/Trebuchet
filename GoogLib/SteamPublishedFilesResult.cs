using Goog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoogLib
{
    public class SteamPublishedFilesResult
    {
        public int result = 0;
        [JsonPropertyName("resultcount")]
        public int resultCount = 0;
        [JsonPropertyName("publishedfiledetails")]
        public List<SteamPublishedFile>? publishedFileDetails = null;
    }
}
