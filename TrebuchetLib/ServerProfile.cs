using System.Text.Json.Serialization;

namespace TrebuchetLib
{
    public class ServerProfile : ProfileFile<ServerProfile>
    {
        public string AdminPassword { get; set; } = AdminPasswordDefault;
        public long CPUThreadAffinity { get; set; } = CPUThreadAffinityDefault;
        public bool DisableHighPrecisionMoveTool { get; set; } = DisableHighPrecisionMoveToolDefault;
        public bool EnableBattleEye { get; set; } = EnableBattleEyeDefault;
        public bool EnableMultiHome { get; set; } = EnableMultiHomeDefault;
        public bool EnableRCon { get; set; } = EnableRConDefault;
        public bool EnableVAC { get; set; } = EnableVACDefault;
        public int GameClientPort { get; set; } = GameClientPortDefault;
        public bool KillZombies { get; set; } = KillZombiesDefault;
        public bool Log { get; set; } = LogDefault;
        public List<string> LogFilters { get; set; } = LogFiltersDefault;
        public string Map { get; set; } = MapDefault;
        public int MaximumTickRate { get; set; } = MaximumTickRateDefault;
        public int MaxPlayers { get; set; } = MaxPlayersDefault;
        public string MultiHomeAddress { get; set; } = MultiHomeAddressDefault;
        public bool NoAISpawn { get; set; } = NoAISpawnDefault;
        public int ProcessPriority { get; set; } = ProcessPriorityDefault;
        public int RConMaxKarma { get; set; } = RConMaxKarmaDefault;
        public string RConPassword { get; set; } = RConPasswordDefault;
        public int RConPort { get; set; } = RConPortDefault;
        public bool RestartWhenDown { get; set; } = RestartWhenDownDefault;
        public string ServerName { get; set; } = ServerNameDefault;
        public string ServerPassword { get; set; } = ServerPasswordDefault;
        public int ServerRegion { get; set; } = ServerRegionDefault;
        public int SourceQueryPort { get; set; } = SourceQueryPortDefault;
        public List<string> SudoSuperAdmins { get; set; } = SudoSuperAdminsDefault;
        public bool UseAllCores { get; set; } = UseAllCoresDefault;
        public int ZombieCheckSeconds { get; set; } = ZombieCheckSecondsDefault;
        public bool AutoRestart { get; set; } = AutoRestartDefault;
        public TimeSpan AutoRestartMinUptime { get; set; } = AutoRestartMinUptimeDefault;
        public List<TimeSpan> AutoRestartDailyTime { get; set; } = AutoRestartDailyTimeDefault;
        public int AutoRestartMaxPerDay { get; set; } = AutoRestartMaxPerDayDefault;
        public string DiscordWebHookNotifications { get; set; } = DiscordWebHookNotificationsDefault;

        
        [JsonIgnore]
        public string ProfileFolder => Path.GetDirectoryName(FilePath) ?? throw new Exception($"Invalid directory for {FilePath}.");

        [JsonIgnore]
        public string ProfileName => Path.GetFileName(Path.GetDirectoryName(FilePath)) ?? string.Empty;
        
        
        /// <summary>
        /// Generate the server arguments for a server instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="modlistPath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetServerArgs(int instance, string modlistPath)
        {
            List<string> args = new List<string>() { Map };
            if (Log) args.Add(Constants.GameArgsLog);
            if (UseAllCores) args.Add(Constants.GameArgsUseAllCore);
            args.Add(string.Format(Constants.ServerArgsMaxPlayers, MaxPlayers));
            args.Add(string.Format(Constants.GameArgsModList, modlistPath));
            if (EnableMultiHome) args.Add(string.Format(Constants.ServerArgsMultiHome, MultiHomeAddress));

            return string.Join(" ", args);
        }
        
        public static readonly string AdminPasswordDefault  = string.Empty;
        public static readonly long CPUThreadAffinityDefault = 0xffffffffffff;
        public static readonly bool DisableHighPrecisionMoveToolDefault = false;
        public static readonly bool EnableBattleEyeDefault = false;
        public static readonly bool EnableMultiHomeDefault = false;
        public static readonly bool EnableRConDefault = false;
        public static readonly bool EnableVACDefault = false;
        public static readonly int GameClientPortDefault = 7777;
        public static readonly bool KillZombiesDefault = false;
        public static readonly bool LogDefault = false;
        public static readonly List<string> LogFiltersDefault =
        [
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
        ];
        public static readonly string MapDefault = "/Game/Maps/ConanSandbox/ConanSandbox";
        public static readonly int MaximumTickRateDefault = 30;
        public static readonly int MaxPlayersDefault = 30;
        public static readonly string MultiHomeAddressDefault = string.Empty;
        public static readonly bool NoAISpawnDefault = false;
        public static readonly int ProcessPriorityDefault = 0;
        public static readonly int RConMaxKarmaDefault = 60;
        public static readonly string RConPasswordDefault = string.Empty;
        public static readonly int RConPortDefault = 25575;
        public static readonly bool RestartWhenDownDefault = false;
        public static readonly string ServerNameDefault = "Conan Exiles Dedicated Server";
        public static readonly string ServerPasswordDefault = string.Empty;
        public static readonly int ServerRegionDefault = 0;
        public static readonly int SourceQueryPortDefault = 27015;
        public static readonly List<string> SudoSuperAdminsDefault = [];
        public static readonly bool UseAllCoresDefault = true;
        public static readonly int ZombieCheckSecondsDefault = 300;
        public static readonly TimeSpan AutoRestartMinUptimeDefault = TimeSpan.FromHours(2);
        public static List<TimeSpan> AutoRestartDailyTimeDefault => [TimeSpan.FromHours(12)];
        public static readonly int AutoRestartMaxPerDayDefault = 0;
        public static readonly bool AutoRestartDefault = false;
        public static readonly string DiscordWebHookNotificationsDefault = string.Empty;
    }
}