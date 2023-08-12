using Goog;
using System.Text.Json.Serialization;

namespace GoogLib
{
    public class ServerProfile : ConfigFile<ServerProfile>
    {
        private bool _killZombies = false;
        private bool _log = false;
        private string _map = "/Game/Maps/ConanSandbox/ConanSandbox";
        private int _maxPlayers = 30;
        private List<string> _modlist = new List<string>();
        private bool _restartWhenDown = false;
        private List<string> _sudoSuperAdmins = new List<string>();
        private bool _useAllCores = true;
        private int _zombieCheckSeconds = 300;

        public bool KillZombies { get => _killZombies; set => _killZombies = value; }

        public bool Log { get => _log; set => _log = value; }

        public string Map { get => _map; set => _map = value; }

        public int MaxPlayers { get => _maxPlayers; set => _maxPlayers = value; }

        public List<string> Modlist { get => _modlist; set => _modlist = value; }

        public bool RestartWhenDown { get => _restartWhenDown; set => _restartWhenDown = value; }

        public List<string> SudoSuperAdmins { get => _sudoSuperAdmins; set => _sudoSuperAdmins = value; }

        public bool UseAllCores { get => _useAllCores; set => _useAllCores = value; }

        public int ZombieCheckSeconds { get => _zombieCheckSeconds; set => _zombieCheckSeconds = value; }

        [JsonIgnore]
        public string ProfileName => Path.GetFileName(Path.GetDirectoryName(FilePath)) ?? string.Empty;

    }
}