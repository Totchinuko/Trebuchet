using System.Collections.Generic;

using CommunityToolkit.Mvvm.Messaging.Messages;
using TrebuchetLib;
using TrebuchetLib.Processes;

namespace Trebuchet
{
    public class InstanceInstalledCountRequest : RequestMessage<int>
    {
    }

    public class ProcessServerDetailsRequest : RequestMessage<List<IConanServerProcess>>
    {
    }

    public class ServerConsoleRequest : RequestMessage<IConsole>
    {
        public int instance;

        public ServerConsoleRequest(int instance)
        {
            this.instance = instance;
        }
    }
}