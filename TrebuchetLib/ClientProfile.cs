using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;
using Yuu.Ini;

namespace TrebuchetLib
{
    public sealed class ClientProfile : ProfileFile<ClientProfile>
    {
        public int AddedTexturePool { get; set; } = 0;

        public bool BackgroundSound { get; set; } = false;

        public int ConfiguredInternetSpeed { get; set; } = 50000;
        public long CPUThreadAffinity { get; set; } = 0xffffffffffff;

        public bool EnableAsyncScene { get; set; } = false;
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

        public float MaxMoveDeltaTime { get; set; } = 0.033f;
        public int ProcessPriority { get; set; } = 0;

        [JsonIgnore]
        public string ProfileFolder => Path.GetDirectoryName(FilePath) ?? throw new Exception($"Invalid directory for {FilePath}.");

        [JsonIgnore]
        public string ProfileName => Path.GetFileName(Path.GetDirectoryName(FilePath)) ?? throw new Exception($"Invalid directory for {FilePath}.");

        public bool RemoveIntroVideo { get; set; } = false;

        public bool TotAdminDoNotLoadServerList { get; set; } = false;

        public bool UltraAnisotropy { get; set; }
        public bool UseAllCores { get; set; } = false;
        
        public string GetClientArgs()
        {
            string profileFolder = Path.GetDirectoryName(FilePath) ?? throw new Exception("Invalid folder directory.");

            List<string> args = new List<string>();
            if (Log) args.Add(Constants.GameArgsLog);
            if (UseAllCores) args.Add(Constants.GameArgsUseAllCore);
            args.Add(string.Format(Constants.GameArgsModList, Path.Combine(profileFolder, Constants.FileGeneratedModlist)));

            return string.Join(" ", args);
        }
    }
}