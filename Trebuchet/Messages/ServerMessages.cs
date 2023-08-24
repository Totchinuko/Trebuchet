using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrebuchetLib;

namespace Trebuchet
{
    public class InstanceInstalledCountRequest : RequestMessage<int>
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

    public class ServerInfoRequest : RequestMessage<List<ServerInstanceInformation>>
    { }

    public abstract class ServerMessages
    {
    }

    public class ServerUpdateMessage : ServerMessages
    { }

    public class ServerUpdateModsMessage : ServerMessages
    {
        public readonly IEnumerable<ulong> modlist;

        public ServerUpdateModsMessage(IEnumerable<ulong> modlist)
        {
            this.modlist = modlist;
        }
    }
}