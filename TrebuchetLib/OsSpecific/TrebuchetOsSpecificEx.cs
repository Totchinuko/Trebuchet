using System.Runtime.InteropServices;
using tot_lib;

namespace TrebuchetLib.OsSpecific;

public static class TrebuchetOsSpecificEx
{
    public static async Task<bool> IsProcessRunning(this ITrebuchetOsSpecific osSpecific, string filename)
    {
        if (!File.Exists(filename)) return false;
        var processList = await osSpecific.GetProcessesWithName(Path.GetFileName(filename));
        foreach (var processData in processList)
        {
            if (processData.filename.Replace("/", "\\").ToLower() == filename.Replace("/", "\\").ToLower())
                return true;
        }
        return false;
    }

    public static ITrebuchetOsSpecific GetOsPlatformSpecific()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new TrebuchetOsWindows();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new TrebuchetOsLinux();
        throw new NotSupportedException("OS not supported");
    }
}