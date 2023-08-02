using Goog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogLib
{
    public class ServerInstance
    {
        private string _profile = string.Empty;

        public string ProfileName
        {
            get => _profile;
            set => _profile = value;
        }
    }
}
