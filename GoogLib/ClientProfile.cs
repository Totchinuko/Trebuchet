using Goog;
using System.Text.Json;

namespace GoogLib
{
    public class ClientProfile : IFile
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true
        };

        private int _addedTexturePool = 0;
        private bool _backgroundSound = false;
        private bool _log = false;
        private string _profileFile = string.Empty;
        private bool _removeIntroVideo = false;
        private bool _useAllCores = true;

        public ClientProfile(string path)
        {
            _profileFile = path;
        }

        public int AddedTexturePool { get => _addedTexturePool; set => _addedTexturePool = value; }

        public bool BackgroundSound { get => _backgroundSound; set => _backgroundSound = value; }

        public bool Log { get => _log; set => _log = value; }

        public string FilePath { get => _profileFile; set => _profileFile = value; }

        public bool RemoveIntroVideo { get => _removeIntroVideo; set => _removeIntroVideo = value; }

        public bool UseAllCores { get => _useAllCores; set => _useAllCores = value; }
    }
}