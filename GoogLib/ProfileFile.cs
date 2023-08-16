using Goog;

namespace GoogLib
{
    public abstract class ProfileFile<T> : JsonFile<T> where T : ProfileFile<T>
    {
        public Config Config { get; protected set; } = default!;

        public static T CreateProfile(Config config, string path)
        {
            var profile = CreateFile(path);
            profile.Config = config;
            return profile;
        }

        public static T LoadProfile(Config config, string path)
        {
            var profile = LoadFile(path);
            profile.Config = config;
            return profile;
        }
    }
}