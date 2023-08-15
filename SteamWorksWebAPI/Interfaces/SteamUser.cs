using SteamWorksWebAPI.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWorksWebAPI.Interfaces
{
    public class SteamUser
    {
        public static async Task<GetPlayerSummariesResponse> GetPlayerSummaries(GetPlayerSummariesQuery query, CancellationToken ct)
        {
            return await SteamWorks.GetAsync<GetPlayerSummariesResponse>(
                SteamWorks.MakeURL(SteamWorks.APIUserHost, "ISteamUser", "GetPlayerSummaries", 2), query, ct)
                ?? new GetPlayerSummariesResponse();
        }
    }
}
