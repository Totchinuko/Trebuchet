using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management;
using System.Runtime.Versioning;

namespace TrebuchetLib.OsSpecific
{
    public struct ProcessData
    {
        public string args;
        public string filename;
        public int pid;
        public string processName;
        public DateTime start;

        [SupportedOSPlatform("windows")]
        public ProcessData(ManagementBaseObject processData)
        {
            pid = Convert.ToInt32(processData.GetPropertyValue("ProcessId"));
            processName = Convert.ToString(processData.GetPropertyValue("Name")) ?? string.Empty;
            start = ManagementDateTimeConverter.ToDateTime(Convert.ToString(processData.GetPropertyValue("CreationDate")) ?? string.Empty).ToUniversalTime();
            string commandLine = Convert.ToString(processData.GetPropertyValue("CommandLine")) ?? string.Empty;

            var arguments = Tools.SplitCommandLine(commandLine).ToArray();

            if (arguments.Length > 0)
                filename = arguments[0].StartsWith("\"") ? arguments[0][1..^1] : arguments[0];
            else
                filename = string.Empty;

            if (arguments.Length > 1)
                args = string.Join(" ", arguments[1..]);
            else
                args = string.Empty;
        }

        [SupportedOSPlatform("linux")]
        public ProcessData(int pid, string commandLine, DateTime start)
        {
            throw new NotImplementedException();
        }

        public static ProcessData Empty => new ProcessData { pid = 0, filename = string.Empty, args = string.Empty };

        public bool IsEmpty => pid == 0;

        public bool TryGetProcess([NotNullWhen(true)] out Process? process)
        {
            process = null;

            try
            {
                process = Process.GetProcessById(pid);
                return true;
            }
            catch { return false; }
        }
    }
}