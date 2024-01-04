using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Trebuchet
{
    public sealed class ModListProfile : ProfileFile<ModListProfile>
    {
        public List<string> Modlist { get; set; } = new List<string>();

        [JsonIgnore]
        public string ProfileName => Path.GetFileNameWithoutExtension(FilePath) ?? string.Empty;

        public string SyncURL { get; set; } = string.Empty;

        /// <summary>
        /// Collect all used mods of all the client and server instances. Can have duplicates.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ulong> CollectAllMods(Config config, IEnumerable<string> modlists)
        {
            foreach (var i in modlists.Distinct())
                if (TryLoadProfile(config, i, out ModListProfile? profile))
                    foreach (var m in profile.Modlist)
                        if (TryParseModID(m, out ulong id))
                            yield return id;
        }

        public static IEnumerable<ulong> CollectAllMods(Config config, string modlist)
        {
            if (TryLoadProfile(config, modlist, out ModListProfile? profile))
                foreach (var m in profile.Modlist)
                    if (TryParseModID(m, out ulong id))
                        yield return id;
        }

        public static IEnumerable<ulong> GetModIDList(IEnumerable<string> modlist)
        {
            foreach (string mod in modlist)
                if (TryParseModID(mod, out ulong id))
                    yield return id;
        }

        public static string GetPath(Config config, string modlistName)
        {
            return Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderModlistProfiles, modlistName + ".json");
        }

        public static IEnumerable<string> ListProfiles(Config config)
        {
            string folder = Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderModlistProfiles);
            if (!Directory.Exists(folder))
                yield break;

            string[] profiles = Directory.GetFiles(folder, "*.json");
            foreach (string p in profiles)
                yield return Path.GetFileNameWithoutExtension(p);
        }

        public static IEnumerable<string> ParseModList(IEnumerable<string> modlist)
        {
            foreach (var mod in modlist)
                if (TryParseModID(mod, out ulong id))
                    yield return id.ToString();
                else
                    yield return mod;
        }

        public static bool ResolveMod(Config config, uint appID, ref string mod)
        {
            string file = mod;
            if (long.TryParse(mod, out _))
                file = Path.Combine(config.InstallPath, Config.FolderWorkshop, appID.ToString(), mod, "none");

            string? folder = Path.GetDirectoryName(file);
            if (folder == null)
                return false;

            if (!long.TryParse(Path.GetFileName(folder), out _))
                return File.Exists(file);

            if (!Directory.Exists(folder))
                return false;

            string[] files = Directory.GetFiles(folder, "*.pak", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
                return false;

            mod = files[0];
            return true;
        }

        public static void ResolveProfile(Config config, ref string profileName)
        {
            if (!string.IsNullOrEmpty(profileName))
            {
                string path = GetPath(config, profileName);
                if (File.Exists(path)) return;
            }

            profileName = Tools.GetFirstFileName(Path.Combine(config.InstallPath, config.VersionFolder, Config.FolderModlistProfiles), "*.json");
            if (!string.IsNullOrEmpty(profileName)) return;

            profileName = "Default";
            if (!File.Exists(GetPath(config, profileName)))
                CreateFile(GetPath(config, profileName)).SaveFile();
        }

        public static bool TryLoadProfile(Config config, string name, [NotNullWhen(true)] out ModListProfile? profile)
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

        public static bool TryParseDirectory2ModID(string path, out ulong id)
        {
            id = 0;
            if (ulong.TryParse(Path.GetFileName(path), out id))
                return true;

            string? parent = Path.GetDirectoryName(path);
            if (parent != null && ulong.TryParse(Path.GetFileName(parent), out id))
                return true;

            return false;
        }

        public static bool TryParseFile2ModID(string path, out ulong id)
        {
            id = 0;
            string? folder = Path.GetDirectoryName(path);
            if (folder == null)
                return false;
            if (ulong.TryParse(Path.GetFileName(folder), out id))
                return true;

            return false;
        }

        public static bool TryParseModID(string path, out ulong id)
        {
            id = 0;
            if (ulong.TryParse(path, out id))
                return true;

            if (Path.GetExtension(path) == ".pak")
                return TryParseFile2ModID(path, out id);
            else
                return TryParseDirectory2ModID(path, out id);
        }

        public IEnumerable<ulong> GetModIDList()
        {
            return GetModIDList(Modlist);
        }

        public string GetModList()
        {
            return string.Join("\r\n", Modlist);
        }

        public IEnumerable<string> GetResolvedModlist()
        {
            foreach (string mod in Modlist)
            {
                string path = mod;
                if (!ResolveMod(ref path))
                    throw new Exception($"Could not resolve mod {path}.");
                yield return path;
            }
        }

        public bool ResolveMod(ref string mod)
        {
            if (ResolveMod(Config, Config.AppIDLiveClient, ref mod))
                return true;
            else if (ResolveMod(Config, Config.AppIDTestLiveClient, ref mod))
                return true;
            return false;
        }

        public void SetModList(string modlist)
        {
            Modlist = modlist.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}