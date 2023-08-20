namespace Trebuchet
{
    public abstract class ConfigFile<T> : JsonFile<T> where T : ConfigFile<T>
    {
        /// <summary>
        /// Create a new config file in memory. Use SaveFile to write it down.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T CreateConfig(string path)
        {
            try
            {
                return CreateFile(path);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create config file.", ex);
            }
        }

        /// <summary>
        /// Load a config file from disk or create a new one if it doesn't exist.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T LoadConfig(string path)
        {
            try
            {
                return LoadFile(path);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load config file.", ex);
            }
        }
    }
}