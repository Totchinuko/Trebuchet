namespace Trebuchet
{
    public abstract class ProfileFile<T> : JsonFile<T> where T : ProfileFile<T>
    {
        protected Config Config { get; set; } = default!;

        /// <summary>
        /// Create a new profile file in memory. Use SaveFile to write it down.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T CreateProfile(Config config, string path)
        {
            try
            {
                var profile = CreateFile(path);
                profile.Config = config;
                return profile;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create profile file.", ex);
            }
        }

        /// <summary>
        /// Load a profile file from disk or create a new one if it doesn't exist.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T LoadProfile(Config config, string path)
        {
            try
            {
                var profile = LoadFile(path);
                profile.Config = config;
                return profile;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load profile file.", ex);
            }
        }
    }
}