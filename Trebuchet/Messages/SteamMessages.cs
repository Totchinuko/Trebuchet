using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Trebuchet.Messages
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