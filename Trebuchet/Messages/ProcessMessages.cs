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
        public ProcessStartedMessage(ProcessData data, int instance = -1) : base(instance)
        {
            this.data = data;
        }

        public ProcessData data { get; } = ProcessData.Empty;
    }

    public class ProcessStoppedMessage : ProcessMessage
    {
        public ProcessStoppedMessage(int instance = -1) : base(instance)
        {
        }
    }
}