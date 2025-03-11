namespace TrebuchetLib
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class IniSettingAttribute : Attribute
    {
        private string _path;

        public IniSettingAttribute(string path, string name)
        {
            _path = string.Format(path, name);
        }

        public string Path => _path;
    }
}