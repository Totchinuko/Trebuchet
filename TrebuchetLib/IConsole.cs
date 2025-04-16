using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tot_lib;

namespace TrebuchetLib
{
    public interface IConsole
    {
        event AsyncEventHandler<ConsoleLogEventArgs>? LogReceived;

        IEnumerable<ConsoleLog> Historic { get; }

        Task SendCommand(string data, CancellationToken ct);
    }

    public class ConsoleLogEventArgs
    {
        public ConsoleLogEventArgs(ConsoleLog consoleLog)
        {
            ConsoleLog = consoleLog;
        }

        public ConsoleLog ConsoleLog { get; }
    }
}