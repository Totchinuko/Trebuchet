using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yuu.Ini;

namespace TrebuchetLib
{
    public class ServerProfile : ProfileFile<ServerProfile>
    {
        public string AdminPassword { get; set; } = string.Empty;

        public long CPUThreadAffinity { get; set; } = 0xffffffffffff;

        public bool DisableHighPrecisionMoveTool { get; set; } = false;
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

        public bool NoAISpawn { get; set; } = false;

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
            if (Log) args.Add(Constants.GameArgsLog);
            if (UseAllCores) args.Add(Constants.GameArgsUseAllCore);
            args.Add(string.Format(Constants.ServerArgsMaxPlayers, MaxPlayers));
            args.Add(string.Format(Constants.GameArgsModList,
                Path.Combine(profileFolder, Constants.FileGeneratedModlist)));
            if (EnableMultiHome) args.Add(string.Format(Constants.ServerArgsMultiHome, MultiHomeAddress));
            args.Add($"-TotInstance={instance}");

            return string.Join(" ", args);
        }
    }
}