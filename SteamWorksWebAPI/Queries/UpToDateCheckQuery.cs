using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWorksWebAPI
{
    public class UpToDateCheckQuery : Query
    {
        public uint AppID { get; set; } = 0;

        public uint Version { get; set; } = 0;
    }
}
    