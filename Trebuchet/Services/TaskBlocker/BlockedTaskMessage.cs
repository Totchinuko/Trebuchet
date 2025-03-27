using TrebuchetUtils;

namespace Trebuchet.Services.TaskBlocker
{
    public abstract class BlockedTaskMessage(IBlockedTaskType type) : ITinyMessage
    {
        public IBlockedTaskType Type { get; } = type;
        public object? Sender { get; } = null;
    }

    public class BlockedTaskStateChanged(IBlockedTaskType type, bool value) : BlockedTaskMessage(type)
    {
        public readonly bool Value = value;
    }
}