using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Trebuchet
{
    public class InstanceInstalledCountRequest : RequestMessage<int>
    {
    }

    public class ServerUpdateMessage
    {
    }

    public class ServerUpdateModsMessage : ServerUpdateMessage
    {
        public readonly IEnumerable<ulong> modlist;

        public ServerUpdateModsMessage(IEnumerable<ulong> modlist)
        {
            this.modlist = modlist;
        }
    }
}