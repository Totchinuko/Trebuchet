using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SteamWorksWebAPI;

namespace TrebuchetUtils
{
    public static class Utils
    {
        public static long Clamp2CpuThreads(long value)
        {
            var maxCpu = Environment.ProcessorCount;
            for (var i = 0; i < 64; i++)
                if (i >= maxCpu)
                    value &= ~(1L << i);
            return value;
        }

        public static void CreateDir(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
        
        public static IEnumerable<(ulong, ulong)> GetManifestKeyValuePairs(this List<PublishedFile> list)
        {
            foreach (var file in list)
            {
                if (ulong.TryParse(file.HcontentFile, out var manifest))
                    yield return (file.PublishedFileID, manifest);
            }
        }

        public static bool UseOsChrome()
        {
            return !OperatingSystem.IsWindows();
        }

        public static void OpenWeb(string url)
        {
            var proc = new Process();
            var infos = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = url
            };
            proc.StartInfo = infos;
            proc.Start();
        }

        public static string GetAllExceptions(this Exception ex)
        {
            var x = 0;
            var pattern = "EXCEPTION #{0}:\r\n{1}";
            var message = String.Format(pattern, ++x, ex.Message);
            message += "\r\n============\r\n" + ex.StackTrace;
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += "\r\n============\r\n" + String.Format(pattern, ++x, inner.Message);
                message += "\r\n============\r\n" + inner.StackTrace;
                inner = inner.InnerException;
            }

            return message;
        }

        public static string GetEmbeddedTextFile(string path)
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(path) ??
                               throw new Exception($"Could not find resource {path}.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static string GetFileVersion()
        {
            if (string.IsNullOrEmpty(Environment.ProcessPath))
                return string.Empty;
            var fvi = FileVersionInfo.GetVersionInfo(Environment.ProcessPath);
            #if DEBUG
            string tag = " [Dev]";
            #else
            string tag = string.Empty;
            #endif
            return $"{fvi.FileMajorPart}.{fvi.FileMinorPart}.{fvi.FileBuildPart}{tag}";
        }

        public static Bitmap LoadFromResource(Uri resourceUri)
        {
            return new Bitmap(AssetLoader.Open(resourceUri));
        }

        public static async Task<Bitmap?> LoadFromWeb(Uri url)
        {
            using var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadAsByteArrayAsync();
                return new Bitmap(new MemoryStream(data));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An error occurred while downloading image '{url}' : {ex.Message}");
                return null;
            }
        }

        public static void DeepCopy(string directory, string destinationDir)
        {
            foreach (var dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
            {
                var dirToCreate = dir.Replace(directory, destinationDir);
                Directory.CreateDirectory(dirToCreate);
            }

            foreach (var newPath in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(directory, destinationDir), true);
            }
        }

        public static async Task DeepCopyAsync(string directory, string destinationDir, CancellationToken token)
        {
            foreach (var dir in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
            {
                var dirToCreate = dir.Replace(directory, destinationDir);
                Directory.CreateDirectory(dirToCreate);
            }

            foreach (var newPath in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                if (token.IsCancellationRequested)
                    return;

                await Task.Run(() => File.Copy(newPath, newPath.Replace(directory, destinationDir), true), token);
            }
        }

        public static void DeleteIfExists(string file)
        {
            if (Directory.Exists(file))
                Directory.Delete(file, true);
            else if (File.Exists(file))
                File.Delete(file);
        }

        public static long DirectorySize(string folder)
        {
            return DirectorySize(new DirectoryInfo(folder));
        }

        public static long DirectorySize(DirectoryInfo folder)
        {
            long size = 0;
            // Add file sizes.
            var fis = folder.GetFiles();
            foreach (var fi in fis)
            {
                size += fi.Length;
            }

            // Add subdirectory sizes.
            DirectoryInfo[] dis = folder.GetDirectories();
            foreach (var di in dis)
            {
                size += DirectorySize(di);
            }

            return size;
        }

        public static string GetFileContent(string path)
        {
            if (!File.Exists(path)) return string.Empty;
            return File.ReadAllText(path);
        }

        public static string GetFirstDirectoryName(string folder, string pattern)
        {
            if (!Directory.Exists(folder))
                return string.Empty;
            var profiles = Directory.GetDirectories(folder, pattern);
            if (profiles.Length == 0)
                return string.Empty;
            return Path.GetFileNameWithoutExtension(profiles[0]);
        }

        public static string GetFirstFileName(string folder, string pattern)
        {
            if (!Directory.Exists(folder))
                return string.Empty;
            var profiles = Directory.GetFiles(folder, pattern);
            if (profiles.Length == 0)
                return string.Empty;
            return Path.GetFileNameWithoutExtension(profiles[0]);
        }

        public static string GetRootPath()
        {
            return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ??
                   throw new DirectoryNotFoundException("Assembly directory is not found.");
        }

        public static T InstantReturn<T>(this ITinyMessengerHub messenger, ITinyInstantReturn<T> msg)
        {
            messenger.Publish(msg);
            return msg.Value;
        }

        public static bool IsDirectoryWritable(string dirPath, bool throwIfFails = false)
        {
            try
            {
                using var fs = File.Create(
                           Path.Combine(
                               dirPath,
                               Path.GetRandomFileName()
                           ),
                           1,
                           FileOptions.DeleteOnClose);

                return true;
            }
            catch
            {
                if (throwIfFails)
                    throw;
                return false;
            }
        }

        public static bool IsSymbolicLink(string path)
        {
            return Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
        }

        public static string PosixFullName(this string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>Reads a null-terminated string into a c# compatible string.</summary>
        /// <param name="input">
        ///     Binary reader to pull the null-terminated string from.  Make sure it is correctly positioned in the
        ///     stream before calling.
        /// </param>
        /// <returns>String of the same encoding as the input BinaryReader.</returns>
        public static string? ReadNullTerminatedString(this BinaryReader input)
        {
            var sb = new StringBuilder();
            var read = input.ReadChar();
            while (read != '\x00')
            {
                sb.Append(read);
                read = input.ReadChar();
            }

            var result = sb.ToString();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        public static string RemoveExtension(this string path)
        {
            return path[..^Path.GetExtension(path).Length];
        }

        public static string RemoveRootFolder(this string path, string root)
        {
            var result = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                .Substring(root.Length);
            return result.StartsWith("\\") ? result.Substring(1) : result;
        }

        public static void RemoveSymbolicLink(string path)
        {
            if (Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                JunctionPoint.Delete(path);
            else if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        public static T? Return<T>(this ITinyMessengerHub messenger, ITinyReturn<T> msg)
        {
            messenger.Publish(msg);
            return msg.ResponseValue;
        }

        public static void SetFileContent(string path, string content)
        {
            var folder = Path.GetDirectoryName(path);
            if (folder == null) throw new Exception($"Invalid folder for {path}.");
            CreateDir(folder);
            File.WriteAllText(path, content);
        }

        public static void SetupSymbolicLink(string path, string targetPath)
        {
            if (Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                JunctionPoint.Delete(path);
            else if (Directory.Exists(path))
                Directory.Delete(path, true);
            JunctionPoint.Create(path, targetPath, true);
        }

        public static IEnumerable<string> Split(this string str, Func<char, bool> controller)
        {
            var nextPiece = 0;

            for (var c = 0; c < str.Length; c++)
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
            var inQuotes = false;

            return commandLine.Split(c =>
                {
                    if (c == '\"')
                        inQuotes = !inQuotes;

                    return !inQuotes && c == ' ';
                })
                .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
                .Where(arg => !string.IsNullOrEmpty(arg));
        }

        /// <summary>
        ///     Subscribe to a message type with the given destination and delivery action.
        ///     All references are held with strong references
        ///     All messages of this type will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="hub"></param>
        /// <param name="recipient">Target object</param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
            ITinyRecipient<TMessage> recipient) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive);
        }

        /// <summary>
        ///     Subscribe to a message type with the given destination and delivery action.
        ///     Messages will be delivered via the specified proxy.
        ///     All references (apart from the proxy) are held with strong references
        ///     All messages of this type will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="hub"></param>
        /// <param name="recipient">Target object</param>
        /// <param name="proxy">Proxy to use when delivering the messages</param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
            ITinyRecipient<TMessage> recipient, ITinyMessageProxy proxy) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive, proxy);
        }

        /// <summary>
        ///     Subscribe to a message type with the given destination and delivery action.
        ///     All messages of this type will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="hub"></param>
        /// <param name="recipient">Target object</param>
        /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
            ITinyRecipient<TMessage> recipient, bool useStrongReferences) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive, useStrongReferences);
        }

        /// <summary>
        ///     Subscribe to a message type with the given destination and delivery action.
        ///     Messages will be delivered via the specified proxy.
        ///     All messages of this type will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="hub"></param>
        /// <param name="recipient">Target object</param>
        /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
        /// <param name="proxy">Proxy to use when delivering the messages</param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
            ITinyRecipient<TMessage> recipient, bool useStrongReferences, ITinyMessageProxy proxy)
            where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive, useStrongReferences, proxy);
        }

        /// <summary>
        ///     Subscribe to a message type with the given destination and delivery action with the given filter.
        ///     All references are held with WeakReferences
        ///     Only messages that "pass" the filter will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="hub"></param>
        /// <param name="recipient">Target object</param>
        /// <param name="messageFilter"></param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
            ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe(recipient.Receive, messageFilter);
        }

        /// <summary>
        ///     Subscribe to a message type with the given destination and delivery action with the given filter.
        ///     Messages will be delivered via the specified proxy.
        ///     All references (apart from the proxy) are held with WeakReferences
        ///     Only messages that "pass" the filter will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="hub"></param>
        /// <param name="recipient">Target object</param>
        /// <param name="messageFilter"></param>
        /// <param name="proxy">Proxy to use when delivering the messages</param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
            ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter, ITinyMessageProxy proxy)
            where TMessage : class, ITinyMessage
        {
            return hub.Subscribe(recipient.Receive, messageFilter, proxy);
        }

        /// <summary>
        ///     Subscribe to a message type with the given destination and delivery action with the given filter.
        ///     All references are held with WeakReferences
        ///     Only messages that "pass" the filter will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="messageFilter"></param>
        /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
        /// <param name="hub"></param>
        /// <param name="recipient"></param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
            ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter, bool useStrongReferences)
            where TMessage : class, ITinyMessage
        {
            return hub.Subscribe(recipient.Receive, messageFilter, useStrongReferences);
        }

        /// <summary>
        ///     Subscribe to a message type with the given destination and delivery action with the given filter.
        ///     Messages will be delivered via the specified proxy.
        ///     All references are held with WeakReferences
        ///     Only messages that "pass" the filter will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="messageFilter"></param>
        /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
        /// <param name="proxy">Proxy to use when delivering the messages</param>
        /// <param name="hub"></param>
        /// <param name="recipient"></param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
            ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter, bool useStrongReferences,
            ITinyMessageProxy proxy) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe(recipient.Receive, messageFilter, useStrongReferences, proxy);
        }

        public static string TrimMatchingQuotes(this string input, char quote)
        {
            if (input.Length >= 2 && input[0] == quote && input[^1] == quote)
                return input[1..^1];

            return input;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dateTime;
        }

        public static void UnzipFile(string file, string destination)
        {
            using var archive = ZipFile.OpenRead(file);
            foreach (var entry in archive.Entries)
                entry.ExtractToFile(Path.Join(destination, entry.FullName));
        }

        public static bool ValidateDirectoryUac(string directory)
        {
            if (!Directory.Exists(directory)) return false;
            return IsDirectoryWritable(directory);
        }

        public static void WriteNullTerminatedString(this BinaryWriter writer, string content)
        {
            var bytes = Encoding.ASCII.GetBytes(content);
            writer.Write(bytes);
            writer.Write((byte)0);
        }

        public static IEnumerable<Type> GetTypesWithAttribute<T>(Assembly? assembly = null) where T : Attribute
        {
            IEnumerable<Type> types = assembly == null ? AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes()) : assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.GetCustomAttributes(typeof(T), true).Length > 0)
                {
                    yield return type;
                }
            }
        }

        public static IEnumerable<(Type type, T attr)> GetTypesWithSingleAttribute<T>(Assembly? assembly = null)
            where T : Attribute
        {
            IEnumerable<Type> types = assembly == null ? AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes()) : assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.GetCustomAttributes(typeof(T), true).FirstOrDefault() is T attr)
                {
                    yield return (type, attr);
                }
            }
        }
        
        public static T? GetAssemblyAttribute<T>(this Type type) where T : Attribute
        {
            Assembly assembly = type.Assembly;
            var attributes = assembly.GetCustomAttributes(typeof(T), true).FirstOrDefault();
            if(attributes is null) return null;
            return (attributes as T);
        }

        public static DirectoryInfo GetStandardFolder(this Type type, Environment.SpecialFolder folder)
        {
            string company = type.GetAssemblyAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;
            if (string.IsNullOrEmpty(company)) company = "tot-software";
            string product = type.GetAssemblyAttribute<AssemblyProductAttribute>()?.Product ?? string.Empty;
            if (string.IsNullOrEmpty(product)) product = "tot-default";
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var directory = new DirectoryInfo(Path.Combine(appData, company, product));
            return directory;
        }
    }
}