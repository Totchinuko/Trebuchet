using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Threading;

namespace Trebuchet
{
    public class SteamConnectionChangedMessage : ValueChangedMessage<bool>
    {
        public SteamConnectionChangedMessage(bool value) : base(value)
        {
        }
    }

    public class SteamConnectMessage
    {
    }
}