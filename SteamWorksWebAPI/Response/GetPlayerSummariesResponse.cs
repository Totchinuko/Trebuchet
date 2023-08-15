using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWorksWebAPI.Response
{
    public class GetPlayerSummariesResponse
    {
        public PlayerSummary[] Players { get; set; } = new PlayerSummary[0];
    }
}
