using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogLib
{
    public class ModlistExport
    {
        private List<string> _modlist = new List<string>();

        public List<string> Modlist { get => _modlist; set => _modlist = value; }
    }
}
