using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoogLib
{
    public class SteamCollectionDetails
    {
        [JsonPropertyName("publishedfileid")]
        public string publishedFileId = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int result = 0;
        public SteamCollectionRow[] children = new SteamCollectionRow[0];
    }
}
