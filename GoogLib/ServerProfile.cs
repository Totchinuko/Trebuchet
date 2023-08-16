using Goog;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yuu.Ini;

namespace GoogLib
{
    public class ServerProfile : ProfileFile<ServerProfile>
    {

        public bool Log { get; set; } = false;

        public string Map { get; set; } = "/Game/Maps/ConanSandbox/ConanSandbox";

        public int MaxPlayers { get; set; } = 30;

        public List<string> SudoSuperAdmins { get; set; } = new List<string>();

        public bool UseAllCores { get; set; } = true;

        #region IniSettings

        [IniSetting(Config.FileIniDefault, "Engine")]
        public void ApplySudoSettings(IniDocument document)
        {
            IniSection section = document.GetSection("/Game/Mods/ModAdmin/Auth/EA_MC_Auth.EA_MC_Auth_C");
            section.GetParameters("+SuperAdminSteamIDs").ForEach(section.Remove);

            foreach (string id in SudoSuperAdmins)
                section.InsertParameter(0, "+SuperAdminSteamIDs", id);
        }

        #endregion IniSettings

        [JsonIgnore]
        public string ProfileFolder => Path.GetDirectoryName(FilePath) ?? throw new Exception($"Invalid directory for {FilePath}.");

        [JsonIgnore]
        public string ProfileName => Path.GetFileName(Path.GetDirectoryName(FilePath)) ?? string.Empty;

        public static string GetFolder(Config config, string name) => Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerProfiles, name);

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

        public static string GetPath(Config config, string name) => Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerProfiles, name, Config.FileProfileConfig);

        public static IEnumerable<string> ListProfiles(Config config)
        {
            string folder = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerProfiles);
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

            profileName = Tools.GetFirstDirectoryName(Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderServerProfiles), "*");
            if (!string.IsNullOrEmpty(profileName)) return;

            profileName = "Default";
            if (!File.Exists(GetPath(config, profileName)))
                CreateFile(GetPath(config, profileName)).SaveFile();
        }

        public string GetServerArgs(int instance)
        {
            string? profileFolder = Path.GetDirectoryName(FilePath) ?? throw new Exception("Invalid folder directory.");

            List<string> args = new List<string>() { Map };
            if (Log) args.Add(Config.GameArgsLog);
            if (UseAllCores) args.Add(Config.GameArgsUseAllCore);
            args.Add(string.Format(Config.ServerArgsMaxPlayers, 10));
            args.Add(string.Format(Config.GameArgsModList, Path.Combine(profileFolder, Config.FileGeneratedModlist)));
            args.Add($"-TotInstance={instance}");

            return string.Join(" ", args);
        }
    }
}