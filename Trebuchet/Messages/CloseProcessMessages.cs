namespace Trebuchet
{
    public class CloseProcessMessage
    {
        public readonly int instance;

        public CloseProcessMessage(int instance)
        {
            this.instance = instance;
        }
    }

    public class KillProcessMessage : CloseProcessMessage
    {
        public KillProcessMessage(int instance) : base(instance)
        {
        }
    }

    public class ShutdownProcessMessage : CloseProcessMessage
    {
        public ShutdownProcessMessage(int instance) : base(instance)
        {
        }
    }
}