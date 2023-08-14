﻿using GoogLib;
using System.Diagnostics;
using System.IO.Compression;
using System.Management;

namespace Goog
{
    public static class Tools
    {
        public static bool CanWriteHere(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            if (!Directory.Exists(path))
                return false;

            try
            {
                string file = Path.Combine(path, "file.lock");
                File.WriteAllText(file, string.Empty);
                File.Delete(file);
                return true;
            }
            catch { return false; }
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

        public static string PosixFullName(this string path) => path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        public static string RemoveExtension(this string path) => path[..^Path.GetExtension(path).Length];

        public static string RemoveRootFolder(this string path, string root)
        {
            string result = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Substring(root.Length);
            return result.StartsWith("\\") ? result.Substring(1) : result;
        }

        public static bool IsSymbolicLink(string path)
        {
            return Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
        }

        public static void RemoveSymboliclink(string path)
        {
            if (Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                Directory.Delete(path);
            else if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        public static void SetupSymboliclink(string path, string targetPath)
        {
            if (Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                Directory.Delete(path);
            else if (Directory.Exists(path))
                Directory.Delete(path, true);
            JunctionPoint.Create(path, targetPath, false);
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

        public static void WriteColored(string text, ConsoleColor color, ConsoleColor background = ConsoleColor.Black)
        {
            Console.ForegroundColor = color;
            Console.BackgroundColor = background;
            Console.Write(text);
            Console.ResetColor();
        }

        public static void WriteColoredLine(string text, ConsoleColor color, ConsoleColor background = ConsoleColor.Black)
        {
            Console.ForegroundColor = color;
            Console.BackgroundColor = background;
            Console.WriteLine(text);
            Console.ResetColor();
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

        public static string GetFirstDirectoryName(string folder, string pattern)
        {
            if (!Directory.Exists(folder))
                return string.Empty;
            string[] profiles = Directory.GetDirectories(folder, pattern);
            if (profiles.Length == 0)
                return string.Empty;
            return Path.GetFileNameWithoutExtension(profiles[0]);
        }

        public static List<ProcessData> GetChildProcesses(int parentId)
        {
            var query = $"Select * From Win32_Process Where ParentProcessId = {parentId}";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            return PackData(processList);
        }

        public static ProcessData GetFirstChildProcesses(int parentId)
        {
            var query = $"Select * From Win32_Process Where ParentProcessId = {parentId}";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            if (processList.Count == 0) return ProcessData.Empty;

            return new ProcessData(processList.Cast<ManagementObject>().First());
        }

        public static ProcessData GetProcess(int processId)
        {
            var query = $"Select * From Win32_Process Where ProcessId = {processId}";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            if (processList.Count == 0) return ProcessData.Empty;

            return new ProcessData(processList.Cast<ManagementObject>().First());
        }

        public static List<ProcessData> GetProcessesWithName(string processName)
        {
            var query = $"Select * From Win32_Process Where Name='{processName}'";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            return PackData(processList);
        }

        public static List<ProcessData> PackData(ManagementObjectCollection collection)
        {
            List<ProcessData> data = new List<ProcessData>(collection.Count);
            foreach (var process in collection)
                data.Add(new ProcessData(process));
            return data;
        }

        public static string TrimMatchingQuotes(this string input, char quote)
        {
            if ((input.Length >= 2) && (input[0] == quote) && (input[input.Length - 1] == quote))
                return input[1..^1];

            return input;
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
    }
}