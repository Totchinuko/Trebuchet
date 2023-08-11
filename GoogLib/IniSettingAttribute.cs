using GoogLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yuu.Ini;

namespace Goog
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
