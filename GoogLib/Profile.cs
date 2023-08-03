using GoogLib;
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
        public static readonly Dictionary<string, string> MapShortcuts = new Dictionary<string, string>
                {
                    { "Exile Land", "/Game/Maps/ConanSandbox/ConanSandbox" },
                    { "Siptah", "/Game/DLC_EXT/DLC_Siptah/Maps/DLC_Isle_of_Siptah" },
                    { "Savage Wild", "/Game/Mods/Savage_Wilds/Savage_Wilds" },
                    { "Sapphire Exile", "/Game/Mods/LCDATest_mapping/LCDAMap" },
                    { "MagMell", "/Game/Mods/MagMell/MagMell" },
                };

        private ClientProfile _client = new ClientProfile();
        private ModListProfile _modlist = new ModListProfile();
        private string _profileFile = string.Empty;
        private ServerProfile _server = new ServerProfile();
        public ClientProfile Client { get => _client; set => _client = value; }

        [JsonIgnore]
        public FileInfo GeneratedModList => new FileInfo(Path.Combine(ProfileFile.DirectoryName ?? "", Config.profileGeneratedModlist));

        public ModListProfile Modlist { get => _modlist; set => _modlist = value; }

        [JsonIgnore]
        public FileInfo ProfileFile => new FileInfo(_profileFile);

        [JsonIgnore]
        public string ProfileName => ProfileFile?.Directory?.Name ?? "";

        public ServerProfile Server { get => _server; set => _server = value; }

        public static void Create(string? path, [NotNull] out Profile? profile)
        {
            profile = null;
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is null or empty");

            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} was not found");

            profile = new Profile();
            profile._profileFile = path;
            profile.SaveProfile();
        }

        public static void Load(string path, [NotNull] out Profile? profile)
        {
            profile = null;
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is null or empty");

            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} was not found");

            string json = File.ReadAllText(path);
            profile = JsonSerializer.Deserialize<Profile>(json);
            if (profile == null)
                throw new NullReferenceException("Could not deserialize profile");
            profile._profileFile = path;
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
                if (File.Exists(path))
                {
                    Load(path, out profile);
                    return;
                }
            }

            throw new ArgumentException("Invalid profile name");
        }

        public static void LoadStrict(bool testlive, string profileName, out Config config, [NotNull] out Profile? profile)
        {
            profile = null;
            Config.Load(out config, testlive);

            if (string.IsNullOrEmpty(profileName))
                throw new ArgumentException("ProfileName is null or empty");
            Load(Path.Combine(config.ProfilesFolder.FullName, profileName, Config.profileConfigName), out profile);
        }

        public static void LoadStrict(bool testlive, string profileName, Config config, [NotNull] out Profile? profile)
        {
            profile = null;
            if (string.IsNullOrEmpty(profileName))
                throw new ArgumentException("ProfileName is null or empty");
            Load(Path.Combine(config.ProfilesFolder.FullName, profileName, Config.profileConfigName), out profile);
        }

        public void CopyTo(string path)
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

            ProfileFile.Directory.CopyTo(target.Directory.FullName);
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
            _profileFile = target.FullName;
            ProfileFile.Refresh();
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