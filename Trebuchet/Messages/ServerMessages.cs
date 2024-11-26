using System.Collections.Generic;

using CommunityToolkit.Mvvm.Messaging.Messages;
using TrebuchetLib;

namespace Trebuchet
{
    public class InstanceInstalledCountRequest : RequestMessage<int>
    {
    }

    public class ProcessServerDetailsRequest : RequestMessage<List<ProcessServerDetails>>
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