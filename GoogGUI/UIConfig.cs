using Goog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI
{
    public sealed class UIConfig : ConfigFile<UIConfig>
    {
        private string _currentClientProfile = string.Empty;
        private string _currentModlistProfile = string.Empty;
        private string _currentServerProfile = string.Empty;
        private bool _useHardwareAcceleration = true;

        public string CurrentClientProfile { get => _currentClientProfile; set => _currentClientProfile = value; }
        public string CurrentModlistProfile { get => _currentModlistProfile; set => _currentModlistProfile = value; }
        public string CurrentServerProfile { get => _currentServerProfile; set => _currentServerProfile = value; }
        public bool UseHardwareAcceleration { get => _useHardwareAcceleration; set => _useHardwareAcceleration = value; }

        public static string GetPath(bool testlive)
        {
            string? ConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(ConfigPath))
                throw new Exception("Path to assembly is invalid.");
            ConfigPath = Path.Combine(ConfigPath, $"{(testlive ? Config.FolderTestLive : Config.FolderLive)}.UIConfig.json");
            return ConfigPath;
        }
    }
}
