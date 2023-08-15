using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWorksWebAPI
{
    public static class PublishedFileService
    {
        public static async Task<QueryFilesResponse> QueryFiles(QueryFilesQuery query, CancellationToken ct)
        {
            return await SteamWorks.GetAsync<QueryFilesResponse>(
                SteamWorks.MakeURL(SteamWorks.APIUserHost, "IPublishedFileService", "QueryFiles", 1),
                query, ct) ?? new QueryFilesResponse();
        }
    }
}
