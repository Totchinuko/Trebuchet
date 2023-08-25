using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrebuchetLib
{
    public class ConsoleLog
    {
        public ConsoleLog(string body, bool isError, bool isReceived)
        {
            UtcTime = DateTime.UtcNow;
            Body = body;
            IsError = isError;
            IsReceived = isReceived;
        }

        public string Body { get; }

        public bool IsError { get; }

        public bool IsReceived { get; }

        public DateTime UtcTime { get; }
    }
}