using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;
using Yuu.Ini;

namespace Trebuchet
{
    public sealed class ClientProfile : ProfileFile<ClientProfile>
    {
        public int AddedTexturePool { get; set; } = 0;

        public bool BackgroundSound { get; set; } = false;

        public long CPUThreadAffinity { get; set; } = 0xffffffffffff;

        public bool Log { get; set; } = false;

        public List<string> LogFilters { get; set; } = new List<string>
        {
          "LogSkinnedMeshComp=NoLogging",
          "NPC=Error",
          "LogLevelActorContainer=NoLogging",
          "LogSkeletalMesh=NoLogging",
          "LogServerStats=NoLogging",
          "LogDataTable=Error",
          "Gamecode_Building=Error",
          "Gamecode_Items=Error",
          "Gamecode_AI=Error",
          "Gamecode_Combat=Error",
          "Gamecode_NPC=Error",
          "Gamecode_Effects=Error",
          "Network=Error",
          "SmokeTest=NoLogging",
          "LogCook=Error",
          "LogSavePackage=Error",
          "LogPackageDependencyInfo=Error",
          "LogTexture=Error",
          "LogStreaming=Error",
          "LogGameMode=Error",
          "HeatmapMetrics=Error",
          "LogUObjectGlobals=Error",
          "AI=Error",
          "ItemInventory=Critical",
          "LogScript=Error",
          "LogNetPackageMap=Error",
          "LogCharacterMovement=Error",
          "LogAnimMontage=Error",
          "Combat=Error",
          "LogStreaming=Critical",
          "LogModController=Error",
          "LogPhysics=Error",
          "Persistence=Error",
          "LogAnimation=Error",
          "SpawnTable=Critical",
          "LogPrimitiveComponent=Error",
          "building=Critical",
          "ConanSandbox=NoLogging",
          "LogScriptCore=Error"
        };

        public int ProcessPriority { get; set; } = 0;

        [JsonIgnore]
        public string ProfileFolder => Path.GetDirectoryName(FilePath) ?? throw new Exception($"Invalid directory for {FilePath}.");

        [JsonIgnore]
        public string ProfileName => Path.GetFileName(Path.GetDirectoryName(FilePath)) ?? throw new Exception($"Invalid directory for {FilePath}.");

        public bool RemoveIntroVideo { get; set; } = false;

        public bool TotAdminDoNotLoadServerList { get; set; } = false;

        public bool UltraAnisotropy { get; set; }

        public bool EnableAsyncScene { get; set; } = false;

        public float MaxMoveDeltaTime { get; set; } = 0.033f;

        public int ConfiguredInternetSpeed { get; set; } = 50000;

        public bool UseAllCores { get; set; } = false;

        #region IniConfig

        [IniSetting(Config.FileIniDefault, "Engine")]
        public void DefaultEngine(IniDocument document)
        {
            document.GetSection("/Game/Mods/TotAdmin/PreLoad/Tot_W_NoServer.Tot_W_NoServer_C").SetParameter("NoServerListAutoRefresh", TotAdminDoNotLoadServerList ? "true" : "false");
        }

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

            IniSection section = document.GetSection("Core.Log");
            section.GetParameters().ForEach(section.Remove);

            document.GetSection("/script/engine.physicssettings")
                .SetParameter("bEnableAsyncScene", EnableAsyncScene ? "True" : "False");

            if (LogFilters.Count > 0)
                foreach (string filter in LogFilters)
                {
                    string[] content = filter.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    section.AddParameter(content[0], content[1]);
                }
            else
                document.Remove(section);
        }

        [IniSetting(Config.FileIniUser, "Game")]
        public void UserGameSetting(IniDocument document)
        {
            document.GetSection("/script/engine.gamenetworkmanager")
                .SetParameter("MaxMoveDeltaTime", MaxMoveDeltaTime.ToString());
            document.GetSection("/script/engine.player")
                .SetParameter("ConfiguredInternetSpeed", ConfiguredInternetSpeed.ToString());
        }

        [IniSetting(Config.FileIniUser, "GraniteBaking")]
        public void Granite(IniDocument document)
        {
            document.GetSection("/script/granitematerialbaker.granitebakingsettings")
                .SetParameter("Quality", UltraAnisotropy ? "High":"Medium");
        }

        [IniSetting(Config.FileIniDefault, "Scalability")]
        public void UltraSetting(IniDocument document)
        {
            document.GetSection("TextureQuality@3").SetParameter("r.Streaming.PoolSize", (1500 + AddedTexturePool).ToString());
            document.GetSection("TextureQuality@3").SetParameter("r.MaxAnisotropy", UltraAnisotropy ? "16" : "8");
        }

        #endregion IniConfig

        public static string GetFolder(Config config, string name) => Path.Combine(config.ResolvedInstallPath, config.VersionFolder, Config.FolderClientProfiles, name);

        public static string GetPath(Config config, string name) => Path.Combine(config.ResolvedInstallPath, config.VersionFolder, Config.FolderClientProfiles, name, Config.FileProfileConfig);

        public static string GetUniqueOriginalProfile(Config config)
        {
            int i = 1;
            while (ProfileExists(config, "_Original_" + i)) i++;
            return GetPath(config, "_Original_" + i);
        }

        public static IEnumerable<string> ListProfiles(Config config)
        {
            string folder = Path.Combine(config.ResolvedInstallPath, config.VersionFolder, Config.FolderClientProfiles);
            if (!Directory.Exists(folder))
                yield break;

            string[] profiles = Directory.GetDirectories(folder, "*");
            foreach (string p in profiles)
                yield return Path.GetFileName(p);
        }

        public static bool ProfileExists(Config config, string name)
        {
            return File.Exists(GetPath(config, name));
        }

        public static void ResolveProfile(Config config, ref string profileName)
        {
            if (!string.IsNullOrEmpty(profileName))
            {
                string path = GetPath(config, profileName);
                if (File.Exists(path)) return;
            }

            profileName = Tools.GetFirstDirectoryName(Path.Combine(config.ResolvedInstallPath, config.VersionFolder, Config.FolderClientProfiles), "*");
            if (!string.IsNullOrEmpty(profileName)) return;

            profileName = "Default";
            if (!File.Exists(GetPath(config, profileName)))
                CreateFile(GetPath(config, profileName)).SaveFile();
        }

        public static bool TryLoadProfile(Config config, string name, [NotNullWhen(true)] out ClientProfile? profile)
        {
            profile = null;
            string profilePath = GetPath(config, name);
            if (!File.Exists(profilePath)) return false;
            try
            {
                profile = LoadProfile(config, profilePath);
                return true;
            }
            catch { return false; }
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

        public string GetClientPath()
        {
            return Config.ClientPath;
        }

        public void WriteIniFiles()
        {
            Dictionary<string, IniDocument> documents = new Dictionary<string, IniDocument>();

            foreach (var method in Tools.GetIniMethod(this))
            {
                IniSettingAttribute attr = method.GetCustomAttribute<IniSettingAttribute>() ?? throw new Exception($"{method.Name} does not have IniSettingAttribute.");
                if (!documents.TryGetValue(attr.Path, out IniDocument? document))
                {
                        document = IniParser.Parse(Tools.GetFileContent(Path.Combine(GetClientPath(), attr.Path)));
                    documents.Add(attr.Path, document);
                }
                method.Invoke(this, new object?[] { document });
            }

            foreach (var document in documents)
            {
                document.Value.MergeDuplicateSections();
                Tools.SetFileContent(Path.Combine(GetClientPath(), document.Key), document.Value.ToString());
            }
        }
    }
}