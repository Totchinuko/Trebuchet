using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Goog
{
    public class Config
    {
        public const string ConfigFilename = "Goog.json";

        public string InstallPath { get; set; }
        public bool ManageServer { get; set; }
        public string ClientPath { get; set; }
        public string LastMap { get; set; }

        [JsonIgnore]
        public bool IsTestLive { get; private set; }

        #region constants
        public const string steamCMDBin = "steamcmd.exe";
        public const string steamFolder = "Steam";
        public const string liveServerFolder = "LiveServer";
        public const string testLiveServerFolder = "TestLiveServer";
        public const string liveServerAppID = "443030";
        public const string testLiveServerAppID = "931580";
        public const string liveClientAppID = "440900";
        public const string testLiveClientAppID = "931180";
        public const string serverBin = "ConanSandboxServer-Win64-Shipping.exe";
        public const string clientBin = "ConanSandbox.exe";
        public const string clientBEBin = "ConanSandbox_BE.exe";
        public const string gameSaveFolder = "ConanSandbox\\Saved";
        public const string gameBinariesFolder = "ConanSandbox\\Binaries\\Win64";
        public const string steamModFolder = "steamapps\\workshop\\content";
        public const string profileFolder = "Profiles";
        public const string testProfileFolder = "TestLiveProfiles";
        public const string profileConfigName = "profile.json";
        public const string profileGeneratedModlist = "modlist.txt";

        public const string CmdArgForceInstallDir = "+force_install_dir {0}";
        public const string CmdArgLoginAnonymous = "+login anonymous";
        public const string CmdArgLogin = "+login {0} {1}";
        public const string CmdArgAppUpdate = "+app_update {0}";
        public const string CmdArgWorkshopUpdate = "+workshop_download_item {0} {1}";
        public const string CmdArgQuit = "+quit";

        public const string GameArgsModList = "-modlist={0}";
        public const string GameArgsLog = "-log";
        public const string GameArgsUseAllCore = "-useallavailablecores";
        public const string ServerArgsMaxPlayers = "-MaxPlayers={0}";
        #endregion

        #region Path
        public DirectoryInfo SteamFolder => new DirectoryInfo(Path.Combine(InstallPath, steamFolder));
        public DirectoryInfo SteamModFolder => new DirectoryInfo(Path.Combine(SteamFolder.FullName, steamModFolder, ClientAppID));
        public FileInfo SteamCMD => new FileInfo(Path.Combine(SteamFolder.FullName, steamCMDBin));

        public DirectoryInfo ProfilesFolder => new DirectoryInfo(Path.Combine(InstallPath, IsTestLive ? testProfileFolder : profileFolder));

        public DirectoryInfo ServerFolder => new DirectoryInfo(Path.Combine(InstallPath, IsTestLive ? testLiveServerFolder : liveServerFolder));
        public DirectoryInfo ServerBinaryFolder => new DirectoryInfo(Path.Combine(ServerFolder.FullName, gameBinariesFolder));
        public DirectoryInfo ServerSaveFolder => new DirectoryInfo(Path.Combine(ServerFolder.FullName, gameSaveFolder));
        public DirectoryInfo ServerOriginalSaveFolder => new DirectoryInfo(Path.Combine(ServerFolder.FullName, gameSaveFolder + "_Original"));
        public FileInfo ServerBin => new FileInfo(Path.Combine(ServerBinaryFolder.FullName, serverBin));

        public DirectoryInfo ClientFolder => new DirectoryInfo(ClientPath);
        public DirectoryInfo ClientBinaries => new DirectoryInfo(Path.Combine(ClientFolder.FullName, gameBinariesFolder));
        public FileInfo ClientBin => new FileInfo(Path.Combine(ClientBinaries.FullName, clientBin));
        public FileInfo ClientBEBin => new FileInfo(Path.Combine(ClientBinaries.FullName, clientBEBin));
        #endregion

        public string ServerAppID => IsTestLive ? testLiveServerAppID : liveServerAppID;
        public string ClientAppID => IsTestLive ? testLiveClientAppID : liveClientAppID;

        public Config()
        {
            InstallPath = "";
            ClientPath = "";
            LastMap = "";
        }

        public static void Load(out Config Config, bool testlive)
        {
            string ConfigPath = GetConfigPath(testlive) ?? "";
            string json = "";
            Config = new Config();

            if (!File.Exists(ConfigPath))
                return;
            json = File.ReadAllText(ConfigPath);
            if (string.IsNullOrEmpty(json))
                throw new NullReferenceException($"No Json to read in {ConfigPath}");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreReadOnlyProperties = true;
            Config = JsonSerializer.Deserialize<Config>(json, options) ?? new Config();
            Config.IsTestLive = testlive;
        }

        public void SaveConfig()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreReadOnlyProperties = true;
            options.WriteIndented = true;
            string json = JsonSerializer.Serialize(this, options);
            string? ConfigPath = GetConfigPath(IsTestLive) ?? "";

            File.WriteAllText(ConfigPath, json);
        }

        public int ResolveModsPath(List<string> modlist, out List<string> resolved, out List<string> errors)
        {
            resolved = new List<string>();
            errors = new List<string>();

            int count = 0;
            foreach (string mod in modlist)
            {
                string path = mod;
                if (ResolveMod(ref path))
                    count++;
                else
                    errors.Add(path);
                resolved.Add(path);
            }

            return count;
        }

        public bool ResolveMod(ref string mod)
        {
            FileInfo file = new FileInfo(mod);

            if(long.TryParse(mod, out _))
                file = new FileInfo(Path.Combine(SteamModFolder.FullName, mod, "Unknown"));

            if (file.Directory == null)
                return false;

            if (!long.TryParse(file.Directory.Name, out _))
                return file.Exists;

            DirectoryInfo dir = new DirectoryInfo(Path.Combine(SteamModFolder.FullName, file.Directory.Name));
            if (!dir.Exists)
                return false;

            string[] files = Directory.GetFiles(dir.FullName, "*.pak", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
                return false;

            mod = files[0];
            return true;
        }

        public void ResolveMap(ref string map, Profile? profile = null)
        {
            if (!string.IsNullOrEmpty(map))
            {
                LastMap = map;
                SaveConfig();
            }

            if (!string.IsNullOrEmpty(profile?.Map))
            {
                map = profile.Map;
                LastMap = map;
                SaveConfig();
            }

            if (!string.IsNullOrEmpty(LastMap))
            {
                map = LastMap;
            }

            throw new Exception("No map could be resolved");
        }


        internal static string? GetConfigPath(bool testlive)
        {
            string? ConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(ConfigPath))
                return null;
            ConfigPath = Path.Combine(ConfigPath, (testlive ? "TestLive." : "") + ConfigFilename);
            return ConfigPath;
        }
    }
}
