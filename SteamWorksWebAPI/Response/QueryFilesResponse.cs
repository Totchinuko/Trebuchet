using SteamWorksWebAPI.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWorksWebAPI
{
    public class QueryFilesResponse
    {
        public int Total { get; set; } = 0;
        public QueriedPublishedFile[] PublishedFileDetails { get; set; } = new QueriedPublishedFile[0];
    }
}
