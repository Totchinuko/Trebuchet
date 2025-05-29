using System.Diagnostics;
using TrebuchetLib.OsSpecific;

namespace TrebuchetLib
{
    public class TrebuchetStartEventArgs : EventArgs
    {
        public int instance;
        public ProcessData process;

        public TrebuchetStartEventArgs(ProcessData process, int instance = -1)
        {
            this.instance = instance;
            this.process = process;
        }
    }
}