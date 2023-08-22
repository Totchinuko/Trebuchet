using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trebuchet
{
    internal interface IServerStateReader
    {
        public ServerState ServerState { get; }
    }
}