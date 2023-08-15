using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SteamWorksWebAPI
{
    public class VoteData
    {
        public double Score { get; set; } = 0;
        [JsonPropertyName("votes_up")]
        public uint VotesUp { get; set; } = 0;
        [JsonPropertyName("votes_down")]
        public uint VotesDown { get; set; } = 0;
    }
}
