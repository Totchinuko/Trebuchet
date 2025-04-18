using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tot_lib;

namespace TrebuchetLib
{
    public interface ITrebuchetConsole
    {
        event AsyncEventHandler<ConsoleLogArgs>? Received;
        Task Send(string data, CancellationToken ct);
    }
}