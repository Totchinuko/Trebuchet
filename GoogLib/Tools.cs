using GoogLib;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Goog
{
    public static class Tools
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true
        };

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

        public static void CreateDir(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
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

        public static T LoadFile<T>(string path) where T : IFile
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} was not found");
            string json = File.ReadAllText(path);
            T? file = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            if (file == null)
                throw new Exception($"{path} could not be loaded");
            file.ProfileFile = path;
            return file;
        }

        public static void SaveFile<T>(this T data) where T : IFile
        {
            string json = JsonSerializer.Serialize(data, typeof(T), _jsonOptions);
            File.WriteAllText(data.ProfileFile, json);
        }

        public static void CopyTo<T>(this T data, string path) where T : IFile
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is invalid");

            string? folder = Path.GetDirectoryName(path);
            if (folder == null)
                throw new DirectoryNotFoundException($"Invalid directory for {path}");

            if (File.Exists(path) || (Directory.Exists(folder)))
                throw new Exception($"{path} already exists");

            string? dataFolder = Path.GetDirectoryName(data.ProfileFile);
            if (dataFolder == null)
                throw new DirectoryNotFoundException($"Invalid directory for {data.ProfileFile}");

            DeepCopy(dataFolder, folder);
        }

        public static void Delete<T>(this T data) where T : IFile
        {
            if (!File.Exists(data.ProfileFile))
                throw new FileNotFoundException($"{data.ProfileFile} not found");
            string? folder = Path.GetDirectoryName(data.ProfileFile);
            if (folder == null || !Directory.Exists(folder))
                throw new DirectoryNotFoundException($"Invalid directory for {data.ProfileFile}");

            Directory.Delete(folder, true);
        }

        public static void MoveTo<T>(this T data, string path) where T : IFile
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is invalid");

            string? targetFolder = Path.GetDirectoryName(path);
            if (targetFolder == null)
                throw new DirectoryNotFoundException($"Invalid directory for {path}");

            if (File.Exists(path) || Directory.Exists(targetFolder))
                throw new Exception($"{path} already exists");

            string? folder = Path.GetDirectoryName(data.ProfileFile);
            if (folder == null)
                throw new DirectoryNotFoundException($"Invalid directory for {data.ProfileFile}");

            Directory.Move(folder, targetFolder);
            data.ProfileFile = path;
        }
    }
}