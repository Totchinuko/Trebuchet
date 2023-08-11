using Goog;
using Yuu.Ini;

namespace GoogLib
{
    public sealed class ClientProfile : ConfigFile<ClientProfile>
    {
        private int _addedTexturePool = 0;
        private bool _backgroundSound = false;
        private bool _log = false;
        private bool _removeIntroVideo = false;
        private bool _ultraAnisotropy;
        private bool _useAllCores = false;
        private bool _useBattleEye = false;

        private ClientProfile()
        { }

        #region Settings

        public int AddedTexturePool { get => _addedTexturePool; set => _addedTexturePool = value; }

        public bool BackgroundSound { get => _backgroundSound; set => _backgroundSound = value; }

        public bool Log { get => _log; set => _log = value; }

        public bool RemoveIntroVideo { get => _removeIntroVideo; set => _removeIntroVideo = value; }

        public bool UltraAnisotropy { get => _ultraAnisotropy; set => _ultraAnisotropy = value; }

        public bool UseAllCores { get => _useAllCores; set => _useAllCores = value; }

        public bool UseBattleEye { get => _useBattleEye; set => _useBattleEye = value; }

        #endregion Settings

        #region IniConfig

        [IniSetting(Config.FileIniDefault, "Game")]
        public void SkipMovies(IniDocument document)
        {
            IniSection section = document.GetSection("/Script/MoviePlayer.MoviePlayerSettings");
            section.GetParameters("+StartupMovies").ForEach(section.Remove);
            if (!RemoveIntroVideo)
            {
                section.AddParameter("+StartupMovies", "StartupUE4");
                section.AddParameter("+StartupMovies", "StartupNvidia");
                section.AddParameter("+StartupMovies", "CinematicIntroV2");
            }
            else
            {
                section.SetParameter("bWaitForMoviesToComplete", "True");
                section.SetParameter("bMoviesAreSkippable", "True");
            }
        }

        [IniSetting(Config.FileIniUser, "Engine")]
        public void SoundSettings(IniDocument document)
        {
            document.GetSection("Audio").SetParameter("UnfocusedVolumeMultiplier", BackgroundSound ? "1.0" : "0,0");
        }

        [IniSetting(Config.FileIniUser, "Scalability")]
        public void UltraSetting(IniDocument document)
        {
            document.GetSection("TextureQuality@3").SetParameter("r.Streaming.PoolSize", (1500 + AddedTexturePool).ToString());
            document.GetSection("TextureQuality@3").SetParameter("r.MaxAnisotropy", UltraAnisotropy ? "16" : "8");
        }

        #endregion IniConfig

        public static string GetPath(Config config, string name) => Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderClientProfiles, name, Config.FileProfileConfig);

        public static void ResolveProfile(Config config, ref string profileName)
        {
            string path = GetPath(config, profileName);
            if (File.Exists(path)) return;

            profileName = Tools.GetFirstFileName(Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderClientProfiles), "*");
            if (!string.IsNullOrEmpty(profileName)) return;

            profileName = "Default";
            CreateFile(GetPath(config, profileName)).SaveFile();
        }

        public static List<string> ListProfiles(Config config)
        {
            List<string> list = new List<string>();
            string folder = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderClientProfiles);
            if (!Directory.Exists(folder))
                return list;

            string[] profiles = Directory.GetDirectories(folder, "*");
            foreach (string p in list)
                list.Add(Path.GetFileName(p));
            return list;
        }

    }
}