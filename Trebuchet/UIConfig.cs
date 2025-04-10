using System;
using System.IO;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet
{
    public sealed class UIConfig : ConfigFile<UIConfig>
    {
        public bool AutoRefreshModlist { get; set; } = AutoRefreshModlistDefault;
        public bool FoldedMenu { get; set; } = false;
        public string CurrentClientProfile { get; set; } = string.Empty;
        public string CurrentModlistProfile { get; set; } = string.Empty;
        public string CurrentServerProfile { get; set; } = string.Empty;
        public bool DisplayProcessPerformance { get; set; } = DisplayProcessPerformanceDefault;
        public bool DisplayWarningOnKill { get; set; } = DisplayWarningOnKillDefault;
        public string UICulture { get; set; } = UICultureDefault;
        
        public static readonly bool DisplayProcessPerformanceDefault = true;
        public static readonly bool DisplayWarningOnKillDefault = true;
        public static readonly bool AutoRefreshModlistDefault = true;
        public static readonly string UICultureDefault = @"en";
    }
}