using System.Diagnostics;
using System.Runtime.Versioning;

namespace TrebuchetLib.OsSpecific;

[SupportedOSPlatform("linux")]
public class TrebuchetOsLinux : ITrebuchetOsSpecific
{
    public IEnumerable<ProcessData> GetChildProcesses(int parentId)
    {
        Process[] processes = Process.GetProcesses();
        foreach (var process in processes)
        {
            var pid = GetParentProcessId(process.Id);
            if (pid == parentId) 
                yield return new ProcessData(process.Id, string.Empty, process.StartTime);
        }
    }

    public ProcessData GetFirstChildProcesses(int parentId)
    {
        var proc = GetChildProcesses(parentId).FirstOrDefault(ProcessData.Empty);
        return proc;
    }

    public ProcessData GetProcess(int processId)
    {
        throw new NotImplementedException();
    }

    public Task<List<ProcessData>> GetProcessesWithName(string processName)
    {
        throw new NotImplementedException();
    }

    public void SetEveryoneAccess(DirectoryInfo dir)
    {
        throw new NotImplementedException();
    }

    public void FocusWindow(IntPtr hwnd)
    {
        throw new NotImplementedException();
    }

    private int GetParentProcessId(int processId)
    {
        string? line;
        using (StreamReader reader = new StreamReader ("/proc/" + processId + "/stat"))
            line = reader.ReadLine();
        if (line == null) return -1;
            
        int endOfName = line.LastIndexOf(')');
        string [] parts = line.Substring(endOfName).Split (new char [] {' '}, 4);

        if (parts.Length >= 3) 
        {
            int ppid = Int32.Parse (parts [2]);
            return ppid;
        }

        return -1;
    }
}
