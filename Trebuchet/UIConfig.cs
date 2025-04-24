using TrebuchetLib;

namespace Trebuchet
{
    public sealed class UIConfig : ConfigFile<UIConfig>
    {
        public bool AutoRefreshModlist { get; set; } = AutoRefreshModlistDefault;
        public bool FoldedMenu { get; set; }
        public string CurrentClientProfile { get; set; } = string.Empty;
        public string CurrentModlistProfile { get; set; } = string.Empty;
        public string CurrentSyncProfile { get; set; } = string.Empty;
        public string CurrentServerProfile { get; set; } = string.Empty;
        public bool DisplayProcessPerformance { get; set; } = DisplayProcessPerformanceDefault;
        public bool DisplayWarningOnKill { get; set; } = DisplayWarningOnKillDefault;
        public string UICulture { get; set; } = UICultureDefault;
        public int PlateformTheme { get; set; } = PlatformThemeDefault;
        public bool Experiments { get; set; } = ExperimentsDefault;
        
        public static readonly bool DisplayProcessPerformanceDefault = true;
        public static readonly bool DisplayWarningOnKillDefault = true;
        public static readonly bool AutoRefreshModlistDefault = true;
        public static readonly string UICultureDefault = string.Empty;
        public static readonly int PlatformThemeDefault = 0;
        public static readonly bool ExperimentsDefault = false;
    }
}