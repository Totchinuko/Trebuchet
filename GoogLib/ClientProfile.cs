using Goog;
using System.Text.Json;

namespace GoogLib
{
    public sealed class ClientProfile : ConfigFile<ClientProfile>
    {
        private int _addedTexturePool = 0;
        private bool _backgroundSound = false;
        private bool _log = false;
        private bool _removeIntroVideo = false;
        private bool _useAllCores = false;

        private ClientProfile() { }

        public int AddedTexturePool { get => _addedTexturePool; set => _addedTexturePool = value; }

        public bool BackgroundSound { get => _backgroundSound; set => _backgroundSound = value; }

        public bool Log { get => _log; set => _log = value; }

        public bool RemoveIntroVideo { get => _removeIntroVideo; set => _removeIntroVideo = value; }

        public bool UseAllCores { get => _useAllCores; set => _useAllCores = value; }

        public static string GetPath(Config config, string name) => Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderClientProfiles, name, Config.FileProfileConfig);

        public static void ResolveProfile(Config config, ref string profileName)
        {
            string path = GetPath(config, profileName);
            if (File.Exists(path)) return;

            profileName = Tools.GetFirstFileName(Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderClientProfiles),  "*");
            if (!string.IsNullOrEmpty(profileName)) return;

            profileName = "Default";
            CreateFile(GetPath(config, profileName)).SaveFile();
        }
    }
}