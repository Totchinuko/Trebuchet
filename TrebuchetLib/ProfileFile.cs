namespace TrebuchetLib
{
    public abstract class ProfileFile<T> : JsonFile<T> where T : ProfileFile<T>
    {
        /// <summary>
        /// Create a new profile file in memory. Use SaveFile to write it down.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static T CreateProfile(string path)
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
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static T LoadProfile(string path)
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

        internal static void RepairMissingProfileFile(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) throw new ArgumentException("path is empty");
                var dir = Path.GetDirectoryName(path) ?? throw new IOException("Profile folder is invalid");
                if (File.Exists(path) || !Directory.Exists(dir)) return;
                var profile = CreateFile(path);
                profile.SaveFile();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to repair broken profile", ex);
            }
        }
    }
}