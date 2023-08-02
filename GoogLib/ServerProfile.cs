using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogLib
{
    public class ServerProfile
    {
        private string _map = "/Game/Maps/ConanSandbox/ConanSandbox";
        private List<string> _modlist = new List<string>();
        private bool _log = false;
        private bool _useAllCores = true;
        private int _maxPlayers = 30;
        private List<string> _sudoSuperAdmins = new List<string>();

        public string Map { get => _map; set => _map = value; }
        public List<string> Modlist { get => _modlist; set => _modlist = value; }
        public bool Log { get => _log; set => _log = value; }
        public bool UseAllCores { get => _useAllCores; set => _useAllCores = value; }
        public int MaxPlayers { get => _maxPlayers; set => _maxPlayers = value; }
        public List<string> SudoSuperAdmins { get => _sudoSuperAdmins; set => _sudoSuperAdmins = value; }
    }
}
