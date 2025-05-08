using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrebuchetLib
{
    public enum ProcessState
    {
        NEW,
        FAILED,
        RUNNING,
        ONLINE,
        CRASHED,
        STOPPING,
        STOPPED,
        FROZEN,
        RESTARTING
    }
}