using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogLib
{
    public class ClientProfile
    {
        private bool _useAllCores = true;
        private bool _log = false;
        private int _addedTexturePool = 0;
        private bool _removeIntroVideo = false;
        private bool _backgroundSound = false;

        public bool UseAllCores { get => _useAllCores; set => _useAllCores = value; }
        public int AddedTexturePool { get => _addedTexturePool; set => _addedTexturePool = value; }
        public bool RemoveIntroVideo { get => _removeIntroVideo; set => _removeIntroVideo = value; }
        public bool BackgroundSound { get => _backgroundSound; set => _backgroundSound = value; }
        public bool Log { get => _log; set => _log = value; }
    }
}
