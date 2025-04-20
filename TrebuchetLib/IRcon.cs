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
        Task<RConResponse> Send(string data, CancellationToken token);
        Task<List<RConResponse>> Send(IEnumerable<string> data, CancellationToken token);
        int QueueData(string data);
        Task<List<RConResponse>> FlushQueue(CancellationToken token);
        Task Disconnect();
    }
}