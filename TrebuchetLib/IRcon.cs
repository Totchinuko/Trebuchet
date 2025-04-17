using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tot_lib;

namespace TrebuchetLib
{
    public interface IRcon
    {
        event AsyncEventHandler<RconEventArgs>? RconResponded;

        Task<int> Send(string data, CancellationToken token);
        Task<List<int>> Send(IEnumerable<string> data, CancellationToken token);
        int QueueData(string data);
        Task FlushQueue(CancellationToken token);
        Task Disconnect();
    }
}