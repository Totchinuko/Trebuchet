using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yuu.Ini;

namespace Trebuchet
{
    public class ServerProfile : ProfileFile<ServerProfile>
    {
        public string AdminPassword { get; set; } = string.Empty;

        public long CPUThreadAffinity { get; set; } = 0xffffffffffff;

        public bool EnableBattleEye { get; set; } = false;

        public bool EnableMultiHome { get; set; } = false;

        public bool EnableRCon { get; set; } = false;

        public bool EnableVAC { get; set; } = false;

        public int GameClientPort { get; set; } = 7777;

        public bool KillZombies { get; set; } = false;

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

        public string Map { get; set; } = "/Game/Maps/ConanSandbox/ConanSandbox";

        public int MaximumTickRate { get; set; } = 30;

        public int MaxPlayers { get; set; } = 30;

        public string MultiHomeAddress { get; set; } = string.Empty;

        public int ProcessPriority { get; set; } = 0;

        [JsonIgnore]
        public string ProfileFolder => Path.GetDirectoryName(FilePath) ?? throw new Exception($"Invalid directory for {FilePath}.");

        [JsonIgnore]
        public string ProfileName => Path.GetFileName(Path.GetDirectoryName(FilePath)) ?? string.Empty;

        /// <summary>
        /// Unused. Kept for Information.
        /// </summary>
        public int RawUPDPort { get; set; } = 7779;

        public int RConMaxKarma { get; set; } = 60;

        public string RConPassword { get; set; } = string.Empty;

        public int RConPort { get; set; } = 25575;

        public bool RestartWhenDown { get; set; } = false;

        public string ServerName { get; set; } = string.Empty;

        public string ServerPassword { get; set; } = string.Empty;

        public int ServerRegion { get; set; } = 0;

        public int SourceQueryPort { get; set; } = 27015;

        public List<string> SudoSuperAdmins { get; set; } = new List<string>();

        public bool UseAllCores { get; set; } = true;

        public int ZombieCheckSeconds { get; set; } = 300;

        #region IniSettings

        [IniSetting(Config.FileIniServer, "Engine")]
        public void ApplyEngineSettings(IniDocument document)
        {
            IniSection section = document.GetSection("OnlineSubsystem");
            section.SetParameter("ServerName", ServerName);
            section.SetParameter("ServerPassword", ServerPassword);

            section = document.GetSection("URL");
            section.SetParameter("Port", GameClientPort.ToString());

            section = document.GetSection("OnlineSubsystemSteam");
            section.SetParameter("GameServerQueryPort", SourceQueryPort.ToString());

            section = document.GetSection("/Script/OnlineSubsystemUtils.IpNetDriver");
            section.SetParameter("NetServerMaxTickRate", MaximumTickRate.ToString());

            section = document.GetSection("Core.Log");
            section.GetParameters().ForEach(section.Remove);

            if (LogFilters.Count > 0)
                foreach (string filter in LogFilters)
                {
                    string[] content = filter.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    section.AddParameter(content[0], content[1]);
                }
            else
                document.Remove(section);
        }

        [IniSetting(Config.FileIniServer, "Game")]
        public void ApplyGameSettings(IniDocument document)
        {
            IniSection section = document.GetSection("/Script/Engine.GameSession");
            section.SetParameter("MaxPlayers", MaxPlayers.ToString());

            section = document.GetSection("RconPlugin");
            section.SetParameter("RconEnabled", EnableRCon.ToString());
            section.SetParameter("RconPort", RConPort.ToString());
            section.SetParameter("RconPassword", RConPassword);
            section.SetParameter("RconMaxKarma", RConMaxKarma.ToString());
        }

        [IniSetting(Config.FileIniServer, "ServerSettings")]
        public void ApplyServerSettings(IniDocument document)
        {
            IniSection section = document.GetSection("ServerSettings");
            section.SetParameter("ServerRegion", ServerRegion.ToString());
            section.SetParameter("AdminPassword", AdminPassword);
            section.SetParameter("IsBattlEyeEnabled", EnableBattleEye.ToString());
            section.SetParameter("IsVACEnabled", EnableVAC.ToString());
        }

        [IniSetting(Config.FileIniDefault, "Engine")]
        public void ApplySudoSettings(IniDocument document)
        {
            IniSection section = document.GetSection("/Game/Mods/ModAdmin/Auth/EA_MC_Auth.EA_MC_Auth_C");
            section.GetParameters("+SuperAdminSteamIDs").ForEach(section.Remove);

            if (SudoSuperAdmins.Count != 0)
                foreach (string id in SudoSuperAdmins)
                    section.InsertParameter(0, "+SuperAdminSteamIDs", id);
            else
                document.Remove(section);
        }

        #endregion IniSettings

        /// <summary>
        /// Get the folder of a server profile.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetFolder(Config config, string name) => Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerProfiles, name);

        /// <summary>
        /// Get the path of a server instance.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static string GetInstancePath(Config config, int instance)
        {
            return Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, instance));
        }

        /// <summary>
        /// Get the executable of a server instance.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static string GetIntanceBinary(Config config, int instance)
        {
            return Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, instance), Config.FileServerProxyBin);
        }

        /// <summary>
        /// Get the map preset list saved in JSon/Maps.json.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Dictionary<string, string> GetMapList()
        {
            string? appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(appFolder)) throw new Exception("Path to assembly is invalid.");

            string file = Path.Combine(appFolder, Config.FileMapJson);
            if (!File.Exists(file)) throw new Exception("Map list file is missing.");

            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
            if (data == null) throw new Exception("Map list could ne be parsed.");

            return data;
        }

        /// <summary>
        /// Get the path of the json file of a server profile.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetPath(Config config, string name) => Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerProfiles, name, Config.FileProfileConfig);

        /// <summary>
        /// List all the server profiles in the installation folder.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IEnumerable<string> ListProfiles(Config config)
        {
            string folder = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerProfiles);
            if (!Directory.Exists(folder))
                yield break;

            string[] profiles = Directory.GetDirectories(folder, "*");
            foreach (string p in profiles)
                yield return Path.GetFileName(p);
        }

        /// <summary>
        /// Resolve a server profile name, if it is not valid, it will be set to the first profile found, if no profile is found, it will be set to "Default" and a new profile will be created.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="profileName"></param>
        public static void ResolveProfile(Config config, ref string profileName)
        {
            if (!string.IsNullOrEmpty(profileName))
            {
                string path = GetPath(config, profileName);
                if (File.Exists(path)) return;
            }

            profileName = Tools.GetFirstDirectoryName(Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerProfiles), "*");
            if (!string.IsNullOrEmpty(profileName)) return;

            profileName = "Default";
            if (!File.Exists(GetPath(config, profileName)))
                CreateFile(GetPath(config, profileName)).SaveFile();
        }

        /// <summary>
        /// Try to load a profile, if it fails, it will return false.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="name"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        public static bool TryLoadProfile(Config config, string name, [NotNullWhen(true)] out ServerProfile? profile)
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

        /// <summary>
        /// Get the path of a server instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public string GetInstancePath(int instance)
        {
            return GetInstancePath(Config, instance);
        }

        /// <summary>
        /// Get the executable of a server instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public string GetIntanceBinary(int instance)
        {
            return GetIntanceBinary(Config, instance);
        }

        /// <summary>
        /// Generate the server arguments for a server instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetServerArgs(int instance)
        {
            string? profileFolder = Path.GetDirectoryName(FilePath) ?? throw new Exception("Invalid folder directory.");

            List<string> args = new List<string>() { Map };
            if (Log) args.Add(Config.GameArgsLog);
            if (UseAllCores) args.Add(Config.GameArgsUseAllCore);
            args.Add(string.Format(Config.ServerArgsMaxPlayers, 10));
            args.Add(string.Format(Config.GameArgsModList, Path.Combine(profileFolder, Config.FileGeneratedModlist)));
            if (EnableMultiHome) args.Add(string.Format(Config.ServerArgsMultiHome, MultiHomeAddress));
            args.Add($"-TotInstance={instance}");

            return string.Join(" ", args);
        }

        /// <summary>
        /// Write the server configuration to the related ini files.
        /// </summary>
        /// <param name="instance"></param>
        /// <exception cref="Exception"></exception>
        public void WriteIniFiles(int instance)
        {
            Dictionary<string, IniDocument> documents = new Dictionary<string, IniDocument>();

            foreach (var method in Tools.GetIniMethod(this))
            {
                IniSettingAttribute attr = method.GetCustomAttribute<IniSettingAttribute>() ?? throw new Exception($"{method.Name} does not have IniSettingAttribute.");
                if (!documents.TryGetValue(attr.Path, out IniDocument? document))
                {
                    document = IniParser.Parse(Tools.GetFileContent(Path.Combine(GetInstancePath(instance), attr.Path)));
                    documents.Add(attr.Path, document);
                }
                method.Invoke(this, new object?[] { document });
            }

            foreach (var document in documents)
            {
                document.Value.MergeDuplicateSections();
                Tools.SetFileContent(Path.Combine(GetInstancePath(instance), document.Key), document.Value.ToString());
            }
        }
    }
}