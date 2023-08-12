using System.Diagnostics;
using System.IO.Compression;

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
    }
}