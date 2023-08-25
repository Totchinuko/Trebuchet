using System;
using TrebuchetLib;

namespace Trebuchet
{
    public class ClientProcessStateChanged : ProcessStateChanged
    {
        public ClientProcessStateChanged(ProcessDetailsEventArgs processDetails)
        {
            ProcessDetails = processDetails;
        }

        public ProcessDetailsEventArgs ProcessDetails { get; }
    }

    public class DashboardStateChanged
    {
    }

    public abstract class ProcessStateChanged
    {
    }

    public class ServerProcessStateChanged : ProcessStateChanged
    {
        public ServerProcessStateChanged(ProcessServerDetailsEventArgs processDetails)
        {
            ProcessDetails = processDetails;
        }

        public ProcessServerDetailsEventArgs ProcessDetails { get; }
    }
}