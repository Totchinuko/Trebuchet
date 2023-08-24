using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrebuchetLib
{
    public interface IConsoleLog
    {
        IEnumerable<ConsoleLog> ConsoleLog { get; }
    }
}