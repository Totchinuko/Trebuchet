using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Trebuchet.Messages
{
    public abstract class OperationMessage
    {
        public readonly Operations key;

        public OperationMessage(Operations key)
        {
            this.key = key;
        }
    }

    public class OperationStateChanged : ValueChangedMessage<bool>
    {
        public readonly Operations key;

        public OperationStateChanged(Operations key, bool value) : base(value)
        {
            this.key = key;
        }
    }
}