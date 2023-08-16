using GoogLib;

namespace Goog
{
    public abstract class ConfigFile<T> : JsonFile<T> where T : ConfigFile<T>
    {
        public static T CreateConfig(string path)
        {
            return CreateFile(path);
        }

        public static T LoadConfig(string path)
        {
            return LoadFile(path);
        }
    }
}