using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Threading;

namespace Trebuchet
{
    public class OperationCancelMessage : OperationMessage
    {
        public OperationCancelMessage(Operations key) : base(key)
        {
        }
    }

    public abstract class OperationMessage
    {
        public readonly Operations key;

        public OperationMessage(Operations key)
        {
            this.key = key;
        }
    }

    public class OperationReleaseMessage : OperationMessage
    {
        public OperationReleaseMessage(Operations key) : base(key)
        {
        }
    }

    public class OperationStartMessage : RequestMessage<CancellationTokenSource>
    {
        public readonly int cancelAfter;
        public readonly Operations key;

        public OperationStartMessage(Operations key, int cancelAfter = 0)
        {
            this.key = key;
            this.cancelAfter = cancelAfter;
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

    public class OperationStateRequest : RequestMessage<bool>
    {
        public readonly Operations[] keys;

        public OperationStateRequest(params Operations[] keys)
        {
            this.keys = keys;
        }
    }
}