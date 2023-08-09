using Goog;
using System.Text.Json;

namespace GoogLib
{
    public class ClientProfile : ConfigFile<ClientProfile>
    {
        private int _addedTexturePool = 0;
        private bool _backgroundSound = false;
        private bool _log = false;
        private bool _removeIntroVideo = false;
        private bool _useAllCores = true;

        public int AddedTexturePool { get => _addedTexturePool; set => _addedTexturePool = value; }

        public bool BackgroundSound { get => _backgroundSound; set => _backgroundSound = value; }

        public bool Log { get => _log; set => _log = value; }

        public bool RemoveIntroVideo { get => _removeIntroVideo; set => _removeIntroVideo = value; }

        public bool UseAllCores { get => _useAllCores; set => _useAllCores = value; }
    }
}