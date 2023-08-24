using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrebuchetLib
{
    public interface IConsole
    {
        event EventHandler<ConsoleLogEventArgs>? LogReceived;

        IEnumerable<ConsoleLog> Historic { get; }

        void SendCommand(string data);
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