using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogLib
{
    public class PastLaunch
    {
        public int Pid { get; set; }
        public string Profile { get; set; }
        public string Modlist { get; set; }

        public PastLaunch(int pid, string profile, string modlist)
        {
            Pid = pid;
            Profile = profile;
            Modlist = modlist;
        }
    }
}
