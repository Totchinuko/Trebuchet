using System.Diagnostics.CodeAnalysis;

namespace TrebuchetLib
{
    public class TrebuchetLaunch : ConfigFile<TrebuchetLaunch>
    {
        public string ModlistName { get; set; } = string.Empty;

        public string ProfileName { get; set; } = string.Empty;

        public static bool TryLoadClientLaunch(Config config, [NotNullWhen(true)] out TrebuchetLaunch? launch)
        {
            launch = null;
            string profilePath = Path.Combine(config.ClientPath, Config.FileTrebuchetLaunch);
            if (!File.Exists(profilePath)) return false;
            try
            {
                launch = LoadConfig(profilePath);
                return true;
            }
            catch { return false; }
        }

        public static bool TryLoadPreviousLaunch(Config config, [NotNullWhen(true)] out ClientProfile? profile, [NotNullWhen(true)] out ModListProfile? modlist)
        {
            profile = null;
            modlist = null;
            if (!TryLoadClientLaunch(config, out TrebuchetLaunch? launch)) return false;
            return ClientProfile.TryLoadProfile(config, launch.ProfileName, out profile) && ModListProfile.TryLoadProfile(config, launch.ModlistName, out modlist);
        }

        public static bool TryLoadPreviousLaunch(Config config, int instance, [NotNullWhen(true)] out ServerProfile? profile, [NotNullWhen(true)] out ModListProfile? modlist)
        {
            profile = null;
            modlist = null;
            if (!TryLoadServerLaunch(config, instance, out TrebuchetLaunch? launch)) return false;
            return ServerProfile.TryLoadProfile(config, launch.ProfileName, out profile) && ModListProfile.TryLoadProfile(config, launch.ModlistName, out modlist);
        }

        public static bool TryLoadServerLaunch(Config config, int instance, [NotNullWhen(true)] out TrebuchetLaunch? launch)
        {
            launch = null;
            string profilePath = Path.Combine(ServerProfile.GetInstancePath(config, instance), Config.FileTrebuchetLaunch);
            if (!File.Exists(profilePath)) return false;
            try
            {
                launch = LoadConfig(profilePath);
                return true;
            }
            catch { return false; }
        }

        public static void WriteConfig(ClientProfile profile, ModListProfile modlist)
        {
            TrebuchetLaunch launch = CreateConfig(Path.Combine(profile.GetClientPath(), Config.FileTrebuchetLaunch));
            launch.ProfileName = profile.ProfileName;
            launch.ModlistName = modlist.ProfileName;
            launch.SaveFile();
        }

        public static void WriteConfig(ServerProfile profile, ModListProfile modlist, int instance)
        {
            TrebuchetLaunch launch = CreateConfig(Path.Combine(profile.GetInstancePath(instance), Config.FileTrebuchetLaunch));
            launch.ProfileName = profile.ProfileName;
            launch.ModlistName = modlist.ProfileName;
            launch.SaveFile();
        }
    }
}