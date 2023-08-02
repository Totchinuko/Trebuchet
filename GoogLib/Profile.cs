using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Goog
{
    public class Profile
    {
        [JsonIgnore]
        public FileInfo ProfileFile { get; private set; }

        [JsonPropertyName("map")]
        public string Map { get; set; }
        [JsonPropertyName("modlist")]
        public List<string> Modlist { get; set; }
        [JsonPropertyName("log")]
        public bool Log { get; set; }
        [JsonPropertyName("server_all_cores")]
        public bool serverUseAllCore { get; set; }
        [JsonPropertyName("client_all_cores")]
        public bool ClientUseAllCore { get; set; }
        [JsonPropertyName("max_player_count")]
        public int MaxPlayerCount { get; set; }

        public static readonly Dictionary<string, string> MapShortcuts = new Dictionary<string, string>
                {
                    { "Exile Land", "/Game/Maps/ConanSandbox/ConanSandbox" },
                    { "Siptah", "/Game/DLC_EXT/DLC_Siptah/Maps/DLC_Isle_of_Siptah" },
                    { "Savage Wild", "/Game/Mods/Savage_Wilds/Savage_Wilds" },
                    { "Sapphire Exile", "/Game/Mods/LCDATest_mapping/LCDAMap" },
                    { "MagMell", "/Game/Mods/MagMell/MagMell" },
                };

        [JsonIgnore]
        public string ProfileName => ProfileFile.Directory?.Name ?? "";

        [JsonIgnore]
        public FileInfo GeneratedModList => new FileInfo(Path.Combine(ProfileFile.DirectoryName ?? "", Config.profileGeneratedModlist));

        public Profile()
        {
            Map = "";
            Modlist = new List<string>();
            ProfileFile = null!;
        }

        public static void Create(string? path, [NotNull] out Profile? profile)
        {
            profile = null;
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is null or empty");

            FileInfo file = new FileInfo(path);
            if (file.Exists)
                throw new FileNotFoundException($"File not found: {file.FullName}");

            profile = new Profile();
            profile.ProfileFile = file;
            profile.SaveProfile();
        }

        public static void Load(string path, [NotNull] out Profile? profile)
        {
            profile = null;
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is null or empty");

            FileInfo file = new FileInfo(path);
            if (!file.Exists)
                throw new FileNotFoundException($"{file.FullName} was not found");

            string json = File.ReadAllText(file.FullName);
            profile = JsonSerializer.Deserialize<Profile>(json);
            if (profile == null)
                throw new NullReferenceException("Could not deserialize profile");
            profile.ProfileFile = file;
        }

        public static void LoadStrict(bool testlive, string profileName, out Config config, [NotNull] out Profile? profile)
        {
            profile = null;
            Config.Load(out config, testlive);

            if (string.IsNullOrEmpty(profileName))
                throw new ArgumentException("ProfileName is null or empty");
            Load(Path.Combine(config.ProfilesFolder.FullName, profileName, Config.profileConfigName), out profile);
        }

        public static void Load(bool testlive, string? profileName, out Config config, [NotNull] out Profile? profile)
        {
            Config.Load(out config, testlive);
            string path = string.Empty;
            if (!string.IsNullOrEmpty(profileName))
            {
                path = Path.Combine(config.ProfilesFolder.FullName, profileName, Config.profileConfigName);
                if (!File.Exists(path))
                    throw new FileNotFoundException($"{path} was not found");
                Load(path, out profile);
                return;
            }

            string? currentFolder = GetCurrentProfileFolder(config);
            if (!string.IsNullOrEmpty(currentFolder))
            {
                path = Path.Combine(config.ProfilesFolder.FullName, currentFolder, Config.profileConfigName);
                if(File.Exists(path))
                {
                    Load(path, out profile);
                    return;
                }
            }

            throw new ArgumentException("Invalid profile name");
        }

        public void SaveProfile()
        {
            if (ProfileFile.Directory == null)
                throw new DirectoryNotFoundException($"Invalid directory for {ProfileFile.FullName}");

            if (!ProfileFile.Directory.Exists)
                Directory.CreateDirectory(ProfileFile.Directory.FullName);

            JsonSerializerOptions option = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, option);
            File.WriteAllText(ProfileFile.FullName, json);
        }

        public void DeleteProfile()
        {
            if (!ProfileFile.Exists)
                throw new FileNotFoundException($"{ProfileFile.FullName} not found");
            DirectoryInfo? dir = ProfileFile.Directory;
            if (dir == null || !dir.Exists)
                throw new DirectoryNotFoundException($"Invalid directory for {ProfileFile.FullName}");

            dir.Delete(true);                
        }

        public void MoveTo(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is invalid");

            FileInfo target = new FileInfo(path);
            if (target.Directory == null)
                throw new DirectoryNotFoundException($"Invalid directory for {target.FullName}");

            if (target.Exists || (target.Directory.Exists))
                throw new Exception($"{target.FullName} already exists");

            if (ProfileFile.Directory == null)
                throw new DirectoryNotFoundException($"Invalid directory for {ProfileFile.FullName}");

            ProfileFile.Directory.MoveTo(target.Directory.FullName);
            ProfileFile = target;
            ProfileFile.Refresh();
        }

        public void CopyTo(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is invalid");

            FileInfo target = new FileInfo(path);
            if(target.Directory == null)
                throw new DirectoryNotFoundException($"Invalid directory for {target.FullName}");

            if (target.Exists || (target.Directory.Exists))
                throw new Exception($"{target.FullName} already exists");

            if (ProfileFile.Directory == null)
                throw new DirectoryNotFoundException($"Invalid directory for {ProfileFile.FullName}");

            ProfileFile.Directory.CopyTo(target.Directory.FullName);
        }

        public void SetModList(string modlist)
        {
            Modlist = modlist.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public string GetModList()
        {
            return string.Join("\r\n", Modlist);
        }

        public List<string> GetModIDList()
        {
            List<string> list = new List<string>();

            foreach (string mod in Modlist)
                if (TryParseModID(mod, out string id))
                    list.Add(id);

            return list;
        }

        public bool TryParseModID(string mod, out string id)
        {
            id = string.Empty;
            if (long.TryParse(mod, out _))
            {
                id = mod;
                return true;
            }
            FileInfo file = new FileInfo(Path.Combine(mod));
            if (file.Directory == null)
                return false;
            if (!long.TryParse(file.Directory.Name, out _))
                return false;
            id = file.Directory.Name;
            return true;
        }

        public bool IsValidModList()
        {
            foreach (string mod in Modlist)
            {
                if (!File.Exists(mod))
                    return false;
            }
            return true;
        }

        public void GenerateModList()
        {
            File.WriteAllLines(GeneratedModList.FullName, Modlist);
        }

        internal static string? GetCurrentProfileFolder(Config config)
        {
            DirectoryInfo current = new DirectoryInfo(Directory.GetCurrentDirectory());
            if (current.FullName.StartsWith(config.ProfilesFolder.FullName))
            {
                string local = current.FullName.RemoveRootFolder(config.ProfilesFolder.FullName);
                string folderName = local.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)[0];
                if (File.Exists(Path.Join(config.ProfilesFolder.FullName, folderName, Config.profileConfigName)))
                    return folderName;
            }
            return null;
        }
    }
}
