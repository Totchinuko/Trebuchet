using System.Diagnostics;
using System.IO.Compression;
using System.Management;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TrebuchetLib;
using Yuu.Ini;

namespace Trebuchet
{
    public static class Tools
    {
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

            foreach (string newPath in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(directory, destinationDir), true);
            }
        }

        public static async Task DeepCopyAsync(string directory, string destinationDir, CancellationToken token)
        {
            foreach (string dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
            {
                string dirToCreate = dir.Replace(directory, destinationDir);
                Directory.CreateDirectory(dirToCreate);
            }

            foreach (string newPath in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                if (token.IsCancellationRequested)
                    return;

                await Task.Run(() => File.Copy(newPath, newPath.Replace(directory, destinationDir), true));
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

        public static long DirectorySize(DirectoryInfo folder)
        {
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

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);

                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
                {
                    var contentLength = response.Content.Headers.ContentLength;
                    if (response.Content.Headers.ContentLength > 1024 * 1024 * 10)
                        throw new Exception("Content was too big.");
                    if (response.Content.Headers.ContentType?.MediaType != "application/json")
                        throw new Exception("Content was not json.");

                    using (var download = await response.Content.ReadAsStreamAsync(ct))
                    {
                        return await JsonSerializer.DeserializeAsync<ModlistExport>(download, new JsonSerializerOptions(), ct) ?? new ModlistExport();
                    }
                }
            }
        }

        public static IEnumerable<ProcessData> EnumerateData(this ManagementObjectCollection collection)
        {
            foreach (var process in collection)
                yield return new ProcessData(process);
        }

        public static IEnumerable<ProcessData> GetChildProcesses(int parentId)
        {
            var query = $"Select * From Win32_Process Where ParentProcessId = {parentId}";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            return processList.EnumerateData();
        }

        public static string GetFileContent(string path)
        {
            if (!File.Exists(path)) return string.Empty;
            return File.ReadAllText(path);
        }

        public static ProcessData GetFirstChildProcesses(int parentId)
        {
            var query = $"Select * From Win32_Process Where ParentProcessId = {parentId}";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            if (processList.Count == 0) return ProcessData.Empty;

            return new ProcessData(processList.Cast<ManagementObject>().First());
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

        public static IEnumerable<MethodInfo> GetIniMethod(object target)
        {
            return target.GetType().GetMethods()
                .Where(meth => meth.GetCustomAttributes(typeof(IniSettingAttribute), true).Any())
                .Where(meth => meth.GetParameters().Length == 1 && meth.GetParameters()[0].ParameterType == typeof(IniDocument));
        }

        public static IEnumerable<KeyValuePair<ulong, FileInfo>> GetModFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                if (ModListProfile.TryParseModID(file, out ulong id))
                    yield return new KeyValuePair<ulong, FileInfo>(id, new FileInfo(file));
            }
        }

        public static ProcessData GetProcess(int processId)
        {
            var query = $"Select * From Win32_Process Where ProcessId = {processId}";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            if (processList.Count == 0) return ProcessData.Empty;

            return new ProcessData(processList.Cast<ManagementObject>().First());
        }

        public static IEnumerable<ProcessData> GetProcessesWithName(string processName)
        {
            var query = $"Select * From Win32_Process Where Name='{processName}'";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            return processList.EnumerateData();
        }

        public static string GetRootPath()
        {
            return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new DirectoryNotFoundException("Assembly directory is not found.");
        }

        public static bool IsClientInstallValid(Config config)
        {
            return !string.IsNullOrEmpty(config.ClientPath) &&
                File.Exists(Path.Combine(config.ClientPath, Config.FolderGameBinaries, Config.FileClientBin));
        }

        public static bool IsDirectoryWritable(string dirPath, bool throwIfFails = false)
        {
            try
            {
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

        public static bool IsProcessRunning(string filename)
        {
            if (!File.Exists(filename)) return false;
            foreach (var processData in GetProcessesWithName(Path.GetFileName(filename)))
            {
                if (processData.filename.Replace("/", "\\").ToLower() == filename.Replace("/", "\\").ToLower())
                    return true;
            }
            return false;
        }

        public static bool IsRunning(this ProcessState state)
        {
            return state == ProcessState.RUNNING || state == ProcessState.STOPPING || state == ProcessState.ONLINE;
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

        public static void SetFileContent(string path, string content)
        {
            string? folder = Path.GetDirectoryName(path);
            if (folder == null) throw new Exception($"Invalid folder for {path}.");
            CreateDir(folder);
            File.WriteAllText(path, content);
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

        public static bool ValidateDirectoryUAC(string directory)
        {
            if (!Directory.Exists(directory)) return false;
            return IsDirectoryWritable(directory, false);
        }

        public static bool ValidateInstallDirectory(string installPath)
        {
            if (string.IsNullOrEmpty(installPath))
                return false;
            if (!Directory.Exists(installPath))
                return false;
            if (Regex.IsMatch(installPath, Config.RegexSavedFolder))
                return false;
            return true;
        }

        public static void WriteNullTerminatedString(this BinaryWriter writer, string content)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(content);
            writer.Write(bytes);
            writer.Write((byte)0);
        }
    }
}