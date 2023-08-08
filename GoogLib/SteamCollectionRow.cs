using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoogLib
{
    public struct SteamCollectionRow
    {
        [JsonPropertyName("publishedfileid")]
        public string publishedFileId;
        [JsonPropertyName("sortorder")]
        public int sortOrder;
        [JsonPropertyName("filetype")]
        public int fileType;
    }
}
