using Goog;

namespace GoogLib
{
    public abstract class ProfileFile<T> : JsonFile<T> where T : ProfileFile<T>
    {
        protected Config Config { get; set; } = default!;

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