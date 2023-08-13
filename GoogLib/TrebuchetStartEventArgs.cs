using System.Diagnostics;

namespace Goog
{
    public class TrebuchetStartEventArgs : EventArgs
    {
        public int intance;
        public ProcessData process;

        public TrebuchetStartEventArgs(Process process, int instance = -1)
        {
            this.intance = instance;
            this.process = Tools.GetProcess(process.Id);
        }
    }
}
