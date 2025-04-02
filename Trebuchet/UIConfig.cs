using System;
using System.IO;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet
{
    public sealed class UIConfig : ConfigFile<UIConfig>
    {
        public bool AutoRefreshModlist { get; set; } = true;
        public bool FoldedMenu { get; set; } = false;
        public string CurrentClientProfile { get; set; } = string.Empty;
        public string CurrentModlistProfile { get; set; } = string.Empty;
        public string CurrentServerProfile { get; set; } = string.Empty;
        public bool DisplayProcessPerformance { get; set; } = true;
        public bool DisplayWarningOnKill { get; set; } = true;
    }
}