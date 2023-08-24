using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrebuchetLib
{
    public class ConsoleLog
    {
        public ConsoleLog(string body, int id)
        {
            UtcTime = DateTime.UtcNow;
            Body = body;
            Id = id;
        }

        public string Body { get; }

        public int Id { get; }

        public DateTime UtcTime { get; }
    }
}