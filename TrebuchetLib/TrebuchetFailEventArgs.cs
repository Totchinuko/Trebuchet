using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trebuchet
{
    public class TrebuchetFailEventArgs : EventArgs
    {
        public Exception Exception { get; }

        public int Instance { get; }

        public TrebuchetFailEventArgs(Exception exception, int instance = -1)
        {
            Exception = exception;
            Instance = instance;
        }
    }
}
