using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWorksWebAPI.Interfaces
{
    public static class SteamRemoteStorage
    {
        public static async Task<CollectionDetailsResponse> GetCollectionDetails(GetCollectionDetailsQuery query, CancellationToken ct)
        {
            return await SteamWorks.PostAsync<CollectionDetailsResponse>(
                SteamWorks.MakeURL(SteamWorks.APIUserHost, "ISteamRemoteStorage", "GetCollectionDetails", 1), query, ct)
                ?? new CollectionDetailsResponse();
        }

        public static async Task<PublishedFilesResponse> GetPublishedFileDetails(GetPublishedFileDetailsQuery query, CancellationToken ct)
        {
            return await SteamWorks.PostAsync<PublishedFilesResponse>(
                SteamWorks.MakeURL(SteamWorks.APIUserHost, "ISteamRemoteStorage", "GetPublishedFileDetails", 1), query, ct)
                ?? new PublishedFilesResponse();
        }
    }
}
