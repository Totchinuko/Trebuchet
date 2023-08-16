using Goog;
using System.Text.Json.Serialization;
using Yuu.Ini;

namespace GoogLib
{
    public sealed class ClientProfile : ProfileFile<ClientProfile>
    {
        private ClientProfile()
        {
        }

        public int AddedTexturePool { get; set; } = 0;

        public bool BackgroundSound { get; set; } = false;

        public bool Log { get; set; } = false;

        [JsonIgnore]
        public string ProfileFolder => Path.GetDirectoryName(FilePath) ?? throw new Exception($"Invalid directory for {FilePath}.");

        [JsonIgnore]
        public string ProfileName => Path.GetFileName(Path.GetDirectoryName(FilePath)) ?? throw new Exception($"Invalid directory for {FilePath}.");

        public bool RemoveIntroVideo { get; set; } = false;

        public bool UltraAnisotropy { get; set; }

        public bool UseAllCores { get; set; } = false;

        public bool UseBattleEye { get; set; } = false;

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

        [IniSetting(Config.FileIniDefault, "Scalability")]
        public void UltraSetting(IniDocument document)
        {
            document.GetSection("TextureQuality@3").SetParameter("r.Streaming.PoolSize", (1500 + AddedTexturePool).ToString());
            document.GetSection("TextureQuality@3").SetParameter("r.MaxAnisotropy", UltraAnisotropy ? "16" : "8");
        }

        #endregion IniConfig

        public static string GetFolder(Config config, string name) => Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderClientProfiles, name);

        public static string GetPath(Config config, string name) => Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderClientProfiles, name, Config.FileProfileConfig);

        public static IEnumerable<string> ListProfiles(Config config)
        {
            string folder = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderClientProfiles);
            if (!Directory.Exists(folder))
                yield break;

            string[] profiles = Directory.GetDirectories(folder, "*");
            foreach (string p in profiles)
                yield return Path.GetFileName(p);
        }

        public static void ResolveProfile(Config config, ref string profileName)
        {
            if (!string.IsNullOrEmpty(profileName))
            {
                string path = GetPath(config, profileName);
                if (File.Exists(path)) return;
            }

            profileName = Tools.GetFirstDirectoryName(Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderClientProfiles), "*");
            if (!string.IsNullOrEmpty(profileName)) return;

            profileName = "Default";
            if (!File.Exists(GetPath(config, profileName)))
                CreateFile(GetPath(config, profileName)).SaveFile();
        }

        public string GetBinaryPath(bool battleEye)
        {
            return Path.Combine(Config.ClientPath, Config.FolderGameBinaries, (battleEye ? Config.FileClientBEBin : Config.FileClientBin));
        }

        public string GetClientArgs()
        {
            string profileFolder = Path.GetDirectoryName(FilePath) ?? throw new Exception("Invalid folder directory.");

            List<string> args = new List<string>();
            if (Log) args.Add(Config.GameArgsLog);
            if (UseAllCores) args.Add(Config.GameArgsUseAllCore);
            args.Add(string.Format(Config.GameArgsModList, Path.Combine(profileFolder, Config.FileGeneratedModlist)));

            return string.Join(" ", args);
        }
    }
}