namespace TrebuchetLib
{
    public abstract class ProfileFile<T> : JsonFile<T> where T : ProfileFile<T>
    {
        /// <summary>
        /// Create a new profile file in memory. Use SaveFile to write it down.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T CreateProfile(string path)
        {
            try
            {
                var profile = CreateFile(path);
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
        public static T LoadProfile(string path)
        {
            try
            {
                var profile = LoadFile(path);
                return profile;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load profile file.", ex);
            }
        }
    }
}