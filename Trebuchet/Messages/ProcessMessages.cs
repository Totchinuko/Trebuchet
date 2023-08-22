using System;

namespace Trebuchet
{
    public class ProcessFailledMessage : ProcessMessage
    {
        public ProcessFailledMessage(Exception ex, int instance = -1) : base(instance)
        {
            Exception = ex;
        }

        public Exception Exception { get; }
    }

    public abstract class ProcessMessage
    {
        internal ProcessMessage(int instance = -1)
        {
            this.instance = instance;
        }

        public int instance { get; } = 0;
    }

    public class ProcessStartedMessage : ProcessMessage
    {
        public readonly ProcessData data;
        public readonly IServerStateReader? reader;

        public ProcessStartedMessage(ProcessData data, int instance = -1, IServerStateReader? reader = null) : base(instance)
        {
            this.data = data;
            this.reader = reader;
        }
    }

    public class ProcessStateChangedMessage
    {
    }

    public class ProcessStoppedMessage : ProcessMessage
    {
        public ProcessStoppedMessage(int instance = -1) : base(instance)
        {
        }
    }
}