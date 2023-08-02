using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goog
{
    static public class Tools
    {
        public static string PosixFullName(this string path) => path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        public static string PosixFullName(this DirectoryInfo path) => path.FullName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        public static string PosixFullName(this FileInfo path) => path.FullName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        public static string RemoveExtension(this string path) => path[..^Path.GetExtension(path).Length];
        public static string RemoveRootFolder(this string path, string root)
        {
            string result = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Substring(root.Length);
            return result.StartsWith("\\") ? result.Substring(1) : result;
        }

        public static void WriteColoredLine(string text, ConsoleColor color, ConsoleColor background = ConsoleColor.Black)
        {
            Console.ForegroundColor = color;        
            Console.BackgroundColor = background;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WriteColored(string text, ConsoleColor color, ConsoleColor background = ConsoleColor.Black)
        {
            Console.ForegroundColor = color;
            Console.BackgroundColor = background;
            Console.Write(text);
            Console.ResetColor();
        }
        public static void CopyTo(this DirectoryInfo directory, string destinationDir)
        {
            foreach (string dir in Directory.GetDirectories(directory.FullName, "*", SearchOption.AllDirectories))
            {
                string dirToCreate = dir.Replace(directory.FullName, destinationDir);
                Directory.CreateDirectory(dirToCreate);
            }

            foreach (string newPath in Directory.GetFiles(directory.FullName, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(directory.FullName, destinationDir), true);
            }
        }

        public static async Task<bool> DownloadSteamCMD(string url, FileInfo file, IProgress<float>? progress)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            Console.WriteLine("Downloading SteamCMD.exe...");
            CancellationToken cancellationToken = default;

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);

                using (FileStream fs = new FileStream(file.FullName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    await client.DownloadAsync(url, fs, progress, cancellationToken);
                }
            }
            return true;
        }

        public static void UnzipFile(FileInfo file, DirectoryInfo destination)
        {
            using (ZipArchive archive = ZipFile.OpenRead(file.FullName))
                foreach (ZipArchiveEntry entry in archive.Entries)
                    entry.ExtractToFile(Path.Join(destination.FullName, entry.FullName));
        }

        public static bool DeleteIfExists(FileInfo file)
        {
            if (file.Exists)
                file.Delete();
            return true;
        }

        public static bool DeleteIfExists(DirectoryInfo directory, bool recursive)
        {
            if (directory.Exists)
                directory.Delete(recursive);

            return true;
        }

        public static bool CreateDir(DirectoryInfo directory)
        {
            if (!directory.Exists)
                Directory.CreateDirectory(directory.FullName);
            return true;
        }

        public static bool SetupSymboliclink(string path, string targetPath)
        {
            DirectoryInfo pathInfo = new DirectoryInfo(path);
            DirectoryInfo targetInfo = new DirectoryInfo(targetPath);

            if (pathInfo.Exists && pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                pathInfo.Delete();
            else if (pathInfo.Exists)
                pathInfo.MoveTo(pathInfo.FullName + "_original");

            JunctionPoint.Create(pathInfo.FullName, targetInfo.FullName, false);

            return true;
        }

        public static bool RemoveSymboliclink(string path)
        {
            DirectoryInfo pathInfo = new DirectoryInfo(path);

            if (pathInfo.Exists && pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                pathInfo.Delete();

            return true;
        }

    }
}
