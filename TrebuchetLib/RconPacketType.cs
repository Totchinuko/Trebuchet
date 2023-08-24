using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrebuchetLib
{
    public enum RconPacketType
    {
        SERVERDATA_AUTH = 3,
        SERVERDATA_AUTH_RESPONSE = 2,
        SERVERDATA_EXECCOMMAND = 2,
        SERVERDATA_RESPONSE_VALUE = 0
    }
}