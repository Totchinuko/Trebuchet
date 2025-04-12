using System.Diagnostics;
using System.IO.Compression;
using System.Management;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using tot_lib;

namespace TrebuchetLib;

public static class Tools
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SetForegroundWindow(IntPtr hwnd);

    public static void FocusWindow(IntPtr hwnd)
    {
        if (OperatingSystem.IsWindows())
            SetForegroundWindow(hwnd);
        else if (OperatingSystem.IsLinux())
            throw new NotImplementedException();
    }
    
    
    
    public static long Clamp2CPUThreads(long value)
    {
        int maxCPU = Environment.ProcessorCount;
        for (int i = 0; i < 64; i++)
            if (i >= maxCPU)
                value &= ~(1L << i);
        return value;
    }

    public static void CreateDir(string directory)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    public static void DeepCopy(string directory, string destinationDir)
    {
        foreach (string dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
        {
            string dirToCreate = dir.Replace(directory, destinationDir);
            Directory.CreateDirectory(dirToCreate);
        }

        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
        foreach (string newPath in files)
        {
            File.Copy(newPath, newPath.Replace(directory, destinationDir), true);
            
        }
    }

    public static async Task DeepCopyAsync(string directory, string destinationDir, CancellationToken token, IProgress<double>? progress = null)
    {
        Directory.CreateDirectory(destinationDir);
        foreach (string dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
        {
            string dirToCreate = dir.Replace(directory, destinationDir);
            Directory.CreateDirectory(dirToCreate);
        }
        var dirInfo = new DirectoryInfo(directory);

        var files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
        if (files.Length == 0) return;
        
        long total = files.Select(f => f.Length).Aggregate((a, b) => a + b);
        long count = 0;
        foreach (FileInfo file in files)
        {
            if (token.IsCancellationRequested)
                return;
            await Task.Run(() => File.Copy(file.FullName, file.FullName.Replace(directory, destinationDir), true));
            count += file.Length;
            progress?.Report((double)count / total);
        }
    }

    public static void RemoveAllJunctions(string directory)
    {
        foreach (string dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
        {
            if (Directory.Exists(dir) && File.GetAttributes(dir).HasFlag(FileAttributes.ReparsePoint))
                RemoveSymboliclink(dir);
        }
    }

    public static void DeleteIfExists(string file)
    {
        if (Directory.Exists(file))
            Directory.Delete(file, true);
        else if (File.Exists(file))
            File.Delete(file);
    }

    public static long DirectorySize(string folder) => DirectorySize(new DirectoryInfo(folder));

    public static long DirectorySize(DirectoryInfo folder)    {
        long size = 0;
        // Add file sizes.
        FileInfo[] fis = folder.GetFiles();
        foreach (FileInfo fi in fis)
        {
            size += fi.Length;
        }
        // Add subdirectory sizes.
        DirectoryInfo[] dis = folder.GetDirectories();
        foreach (DirectoryInfo di in dis)
        {
            size += DirectorySize(di);
        }
        return size;
    }

    public static async Task<ModlistExport> DownloadModList(string url, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("Sync URL is invalid");

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        var contentLength = response.Content.Headers.ContentLength;
        if (response.Content.Headers.ContentLength > 1024 * 1024 * 10)
            throw new Exception("Content was too big.");
        if (response.Content.Headers.ContentType?.MediaType != "application/json")
            throw new Exception("Content was not json.");

        await using var download = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<ModlistExport>(download, new JsonSerializerOptions(), ct) ?? new ModlistExport();
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<ProcessData> EnumerateData(this ManagementObjectCollection collection)
    {
        foreach (var process in collection)
            yield return new ProcessData(process);
    }

    public static IEnumerable<ProcessData> GetChildProcesses(int parentId)
    {
        if(OperatingSystem.IsWindows())
            return GetChildProcessesWindows(parentId);
        else if (OperatingSystem.IsLinux())
            return GetChildProcessesLinux(parentId);
        throw new NotSupportedException("Operating system is not supported.");
    }
        
    [SupportedOSPlatform("windows")]
    private static IEnumerable<ProcessData> GetChildProcessesWindows(int parentId)
    {
        var query = $"Select * From Win32_Process Where ParentProcessId = {parentId}";
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
        ManagementObjectCollection processList = searcher.Get();

        return processList.EnumerateData();
    }
        
    //TODO: Find a way to recover the command line of the process post launch
    [SupportedOSPlatform("linux")]
    private static IEnumerable<ProcessData> GetChildProcessesLinux(int parentId)
    {
        Process[] processes = Process.GetProcesses();
        foreach (var process in processes)
        {
            var pid = GetParentProcessIdLinux(process.Id);
            if (pid == parentId) 
                yield return new ProcessData(process.Id, string.Empty, process.StartTime);
        }
    }

    public static async Task<string> GetFileContent(string path)
    {
        if (!File.Exists(path)) return string.Empty;
        return await File.ReadAllTextAsync(path);
    }
        
    public static ProcessData GetFirstChildProcesses(int parentId)
    {
        if(OperatingSystem.IsWindows())
            return GetFirstChildProcessesWindows(parentId);
        else if (OperatingSystem.IsLinux())
            return GetFirstChildProcessesLinux(parentId);
        throw new NotSupportedException("Operating system is not supported.");
    }

    [SupportedOSPlatform("windows")]
    private static ProcessData GetFirstChildProcessesWindows(int parentId)
    {
        var query = $"Select * From Win32_Process Where ParentProcessId = {parentId}";
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
        ManagementObjectCollection processList = searcher.Get();

        if (processList.Count == 0) return ProcessData.Empty;

        return new ProcessData(processList.Cast<ManagementObject>().First());
    }

    [SupportedOSPlatform("linux")]
    private static ProcessData GetFirstChildProcessesLinux(int parentId)
    {
        var proc = GetChildProcessesLinux(parentId).FirstOrDefault(ProcessData.Empty);
        return proc;
    }

    public static string GetFirstDirectoryName(string folder, string pattern)
    {
        if (!Directory.Exists(folder))
            return string.Empty;
        string[] profiles = Directory.GetDirectories(folder, pattern);
        if (profiles.Length == 0)
            return string.Empty;
        return Path.GetFileNameWithoutExtension(profiles[0]);
    }

    public static string GetFirstFileName(string folder, string pattern)
    {
        if (!Directory.Exists(folder))
            return string.Empty;
        string[] profiles = Directory.GetFiles(folder, pattern);
        if (profiles.Length == 0)
            return string.Empty;
        return Path.GetFileNameWithoutExtension(profiles[0]);
    }

    public static ProcessData GetProcess(int processId)
    {
        if(OperatingSystem.IsWindows())
            return GetProcessWindows(processId);
        else if (OperatingSystem.IsLinux())
            return GetProcessLinux(processId);
        throw new NotSupportedException("Operating system is not supported.");
    }
        
    [SupportedOSPlatform("windows")]
    private static ProcessData GetProcessWindows(int processId)
    {
        var query = $"Select * From Win32_Process Where ProcessId = {processId}";
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
        ManagementObjectCollection processList = searcher.Get();

        if (processList.Count == 0) return ProcessData.Empty;

        return new ProcessData(processList.Cast<ManagementObject>().First());
    }

    [SupportedOSPlatform("linux")]
    private static ProcessData GetProcessLinux(int processId)
    {
        // TODO: Linux process discovery
        return ProcessData.Empty;
    }

    public static Task<List<ProcessData>> GetProcessesWithName(string processName)
    {
        if(OperatingSystem.IsWindows())
            return GetProcessesWithNameWindows(processName);
        else  if (OperatingSystem.IsLinux())
            return GetProcessesWithNameLinux(processName);
        throw new NotSupportedException("Operating system is not supported.");
    }

    [SupportedOSPlatform("windows")]
    private static async Task<List<ProcessData>> GetProcessesWithNameWindows(string processName)
    {
        return await Task.Run(() =>
        {
            var query = $"Select * From Win32_Process Where Name='{processName}'";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            return processList.EnumerateData().ToList();
        });
    }

    [SupportedOSPlatform("linux")]
    private static Task<List<ProcessData>> GetProcessesWithNameLinux(string processName)
    {
        // TODO: Linux process discovery
        throw new NotImplementedException();
    }
        
    [SupportedOSPlatform("linux")]
    private static int GetParentProcessIdLinux(int processId)
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

    public static string GetRootPath()
    {
        return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new DirectoryNotFoundException("Assembly directory is not found.");
    }

    public static bool IsClientInstallValid(Config config)
    {
        return IsClientInstallValid(config.ClientPath);
    }

    public static bool IsClientInstallValid(string directory)
    {
        return !string.IsNullOrEmpty(directory) &&
               File.Exists(Path.Combine(directory, Constants.FolderGameBinaries, Constants.FileClientBin));
    }

    public static bool IsDirectoryWritable(string dirPath, bool throwIfFails = false)
    {
        try
        {
            if(!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            using (FileStream fs = File.Create(
                       Path.Combine(
                           dirPath,
                           Path.GetRandomFileName()
                       ),
                       1,
                       FileOptions.DeleteOnClose)
                  )
            { }
            return true;
        }
        catch
        {
            if (throwIfFails)
                throw;
            else
                return false;
        }
    }

    public static async Task<bool> IsProcessRunning(string filename)
    {
        if (!File.Exists(filename)) return false;
        var processList = await GetProcessesWithName(Path.GetFileName(filename));
        foreach (var processData in processList)
        {
            if (processData.filename.Replace("/", "\\").ToLower() == filename.Replace("/", "\\").ToLower())
                return true;
        }
        return false;
    }

    public static bool IsRunning(this ProcessState state)
    {
        return state is 
            ProcessState.RUNNING or 
            ProcessState.STOPPING or 
            ProcessState.ONLINE or 
            ProcessState.FROZEN;
    }

    public static bool IsServerInstallValid(Config config)
    {
        return config.ServerInstanceCount > 0;
    }

    public static bool IsSymbolicLink(string path)
    {
        return Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
    }

    public static string PosixFullName(this string path) => path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>Reads a null-terminated string into a c# compatible string.</summary>
    /// <param name="input">Binary reader to pull the null-terminated string from.  Make sure it is correctly positioned in the stream before calling.</param>
    /// <returns>String of the same encoding as the input BinaryReader.</returns>
    public static string? ReadNullTerminatedString(this BinaryReader input)
    {
        StringBuilder sb = new StringBuilder();
        char read = input.ReadChar();
        while (read != '\x00')
        {
            sb.Append(read);
            read = input.ReadChar();
        }
        string result = sb.ToString();
        return string.IsNullOrEmpty(result) ? null : result;
    }

    public static string RemoveExtension(this string path) => path[..^Path.GetExtension(path).Length];

    public static string RemoveRootFolder(this string path, string root)
    {
        string result = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Substring(root.Length);
        return result.StartsWith("\\") ? result.Substring(1) : result;
    }

    public static void RemoveSymboliclink(string path)
    {
        if (Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            JunctionPoint.Delete(path);
        else if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    public static async Task SetFileContent(string path, string content)
    {
        string? folder = Path.GetDirectoryName(path);
        if (folder == null) throw new Exception($"Invalid folder for {path}.");
        CreateDir(folder);
        await File.WriteAllTextAsync(path, content);
    }

    public static void SetupSymboliclink(string path, string targetPath)
    {
        if (Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            JunctionPoint.Delete(path);
        else if (Directory.Exists(path))
            Directory.Delete(path, true);
        JunctionPoint.Create(path, targetPath, true);
    }

    public static IEnumerable<string> Split(this string str, Func<char, bool> controller)
    {
        int nextPiece = 0;

        for (int c = 0; c < str.Length; c++)
        {
            if (controller(str[c]))
            {
                yield return str.Substring(nextPiece, c - nextPiece);
                nextPiece = c + 1;
            }
        }

        yield return str.Substring(nextPiece);
    }

    public static IEnumerable<string> SplitCommandLine(string commandLine)
    {
        bool inQuotes = false;

        return commandLine.Split(c =>
            {
                if (c == '\"')
                    inQuotes = !inQuotes;

                return !inQuotes && c == ' ';
            })
            .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
            .Where(arg => !string.IsNullOrEmpty(arg));
    }

    public static string TrimMatchingQuotes(this string input, char quote)
    {
        if ((input.Length >= 2) && (input[0] == quote) && (input[input.Length - 1] == quote))
            return input[1..^1];

        return input;
    }

    public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }

    public static void UnzipFile(string file, string destination)
    {
        using (ZipArchive archive = ZipFile.OpenRead(file))
            foreach (ZipArchiveEntry entry in archive.Entries)
                entry.ExtractToFile(Path.Join(destination, entry.FullName));
    }

    public static bool IsProcessElevated()
    {
        if (OperatingSystem.IsWindows())
            return IsProcessElevatedWindows();
        return false;
    }
    

    [SupportedOSPlatform("windows")]
    private static bool IsProcessElevatedWindows()
    {
        return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static bool ValidateInstallDirectory(string installPath)
    {
        if (string.IsNullOrEmpty(installPath))
            return false;
        if (!Directory.Exists(installPath))
            return false;
        if (Regex.IsMatch(installPath, Constants.RegexSavedFolder))
            return false;
        return true;
    }

    public static void WriteNullTerminatedString(this BinaryWriter writer, string content)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(content);
        writer.Write(bytes);
        writer.Write((byte)0);
    }
        
            
    /// <summary>
    /// Get the map preset list saved in JSon/Maps.json.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Dictionary<string, string> GetMapList()
    {
        string? appFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        if (string.IsNullOrEmpty(appFolder)) throw new Exception("Path to assembly is invalid.");

        string file = Path.Combine(appFolder, Constants.FileMapJson);
        if (!File.Exists(file)) throw new Exception("Map list file is missing.");

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
        if (data == null) throw new Exception("Map list could ne be parsed.");

        return data;
    }
    
    /// <summary>
    /// Set Everyone Full Control permissions for selected directory
    /// </summary>
    /// <param name="dirName"></param>
    /// <returns></returns>
    public static bool SetEveryoneAccess(string dirName)
    {
       if(OperatingSystem.IsWindows())
           return SetEveryoneAccessWindows(dirName);
       return true;
    }

    public static void SetEveryoneAccess(DirectoryInfo dir)
    {
        if(OperatingSystem.IsWindows())
            SetEveryoneAccessWindows(dir);
    }
    
    [SupportedOSPlatform("windows")]
    private static bool SetEveryoneAccessWindows(string dirName)
    {
        // Make sure directory exists
        if (!Directory.Exists(dirName))
            return false;
        // Get directory access info
        DirectoryInfo dinfo = new DirectoryInfo(dirName);
        DirectorySecurity dSecurity = dinfo.GetAccessControl();
        // Add the FileSystemAccessRule to the security settings. 
        dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
        // Set the access control
        dinfo.SetAccessControl(dSecurity);

        return true;
    }

    [SupportedOSPlatform("windows")]
    private static void SetEveryoneAccessWindows(DirectoryInfo dir)
    {
        SetEveryoneAccessWindows(dir.GetDirectories("*", SearchOption.AllDirectories));
        SetEveryoneAccessWindows(dir.GetFiles("*", SearchOption.AllDirectories));
    }
    
    [SupportedOSPlatform("windows")]
    private static void SetEveryoneAccessWindows(DirectoryInfo[] dirs)
    {
        foreach (var dir in dirs)
        {
            DirectorySecurity dSecurity = dir.GetAccessControl();
            // Add the FileSystemAccessRule to the security settings. 
            dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
            // Set the access control
            dir.SetAccessControl(dSecurity);
        }
    }
    
    [SupportedOSPlatform("windows")]
    private static void SetEveryoneAccessWindows(FileInfo[] files)
    {
        foreach (var file in files)
        {
            FileSecurity dSecurity = file.GetAccessControl();
            // Add the FileSystemAccessRule to the security settings. 
            dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
            // Set the access control
            file.SetAccessControl(dSecurity);
        }
    }
}