using Goog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Yuu.Ini;

namespace GoogLib
{
    public class IniConfigurator
    {
        private Config _config;
        private Dictionary<string, IniDocument> _iniDocuments = new Dictionary<string, IniDocument>();
        private IniParserConfiguration _parserConfiguration = new IniParserConfiguration
        {
        };

        public IniConfigurator(Config config)
        {
            _config = config;
        }

        public void WriteIniConfigs(object target, string gamePath) 
        {
            var methods = target.GetType().GetMethods()
                .Where(meth => meth.GetCustomAttributes(typeof(IniSettingAttribute), true).Any())
                .Where(meth => meth.GetParameters().Length == 1 && meth.GetParameters()[0].ParameterType == typeof(IniDocument));

            foreach(var method in methods)
            {
                IniSettingAttribute attr = method.GetCustomAttribute<IniSettingAttribute>() ?? throw new Exception($"{method.Name} does not have IniSettingAttribute.");
                IniDocument document = GetIniDocument(Path.Combine(gamePath, attr.Path));
                method.Invoke(target, new object?[] { document });
            }
        }

        public void FlushConfigs()
        {
            foreach(var document in _iniDocuments)
            {
                document.Value.MergeDuplicateSections();
                SetIniFile(document.Key, document.Value.ToString());
            }
        }

        private string GetIniFile(string path)
        {
            if (!Directory.Exists(_config.ClientPath)) throw new DirectoryNotFoundException("Game path is not found.");

            path = Path.Combine(_config.ClientPath, path);
            if (!File.Exists(path)) return string.Empty;

            return File.ReadAllText(path);
        }

        public void SetIniFile(string path, string content)
        {
            if (!Directory.Exists(_config.ClientPath)) throw new DirectoryNotFoundException("Game path is not found.");

            path = Path.Combine(_config.ClientPath, path);
            File.WriteAllText(path, content);
        }

        private IniDocument GetIniDocument(string path)
        {
            if (_iniDocuments.TryGetValue(path, out var parser))
                return parser;

            string content = GetIniFile(path);
            IniDocument document = IniParser.Parse(content, _parserConfiguration);
            return document;
        }
    }
}
