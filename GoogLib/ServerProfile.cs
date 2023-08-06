using Goog;
using System.Text.Json;

namespace GoogLib
{
    public class ServerProfile : IFile
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true
        };

        private bool _log = false;
        private string _map = "/Game/Maps/ConanSandbox/ConanSandbox";
        private int _maxPlayers = 30;
        private List<string> _modlist = new List<string>();
        private string _profileFile = string.Empty;
        private List<string> _sudoSuperAdmins = new List<string>();
        private bool _useAllCores = true;

        public ServerProfile(string path)
        {
            _profileFile = path;
        }

        public bool Log { get => _log; set => _log = value; }

        public string Map { get => _map; set => _map = value; }

        public int MaxPlayers { get => _maxPlayers; set => _maxPlayers = value; }

        public List<string> Modlist { get => _modlist; set => _modlist = value; }

        public List<string> SudoSuperAdmins { get => _sudoSuperAdmins; set => _sudoSuperAdmins = value; }

        public bool UseAllCores { get => _useAllCores; set => _useAllCores = value; }
        public string ProfileFile { get => _profileFile; set => _profileFile = value; }
    }
}