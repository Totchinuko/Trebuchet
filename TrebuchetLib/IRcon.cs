using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrebuchetLib
{
    public interface IRcon
    {
        event EventHandler<RconEventArgs>? RconResponded;

        int Send(string data);

        IEnumerable<int> Send(IEnumerable<string> data);
    }
}