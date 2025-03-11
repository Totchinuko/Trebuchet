using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrebuchetUtils
{
    public static class Utils
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

        public static void OpenWeb(string url)
        {
            var proc = new Process();
            var infos = new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = url
            };
            proc.StartInfo = infos;
            proc.Start();
        }

        public static string GetAllExceptions(this Exception ex)
        {
            int x = 0;
            string pattern = "EXCEPTION #{0}:\r\n{1}";
            string message = String.Format(pattern, ++x, ex.Message);
            message += "\r\n============\r\n" + ex.StackTrace;
            Exception? inner = ex.InnerException;
            while (inner != null)
            {
                message += "\r\n============\r\n" + String.Format(pattern, ++x, inner.Message);
                message += "\r\n============\r\n" + inner.StackTrace;
                inner = inner.InnerException;
            }
            return message;
        }

        public static string GetEmbededTextFile(string path)
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(path) ?? throw new Exception($"Could not find resource {path}."))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetFileVersion()
        {
            if (string.IsNullOrEmpty(System.Environment.ProcessPath))
                return string.Empty;
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Environment.ProcessPath);
            return fvi.FileVersion ?? string.Empty;
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

        public static string GetFileContent(string path)
        {
            if (!File.Exists(path)) return string.Empty;
            return File.ReadAllText(path);
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

        public static string GetRootPath()
        {
            return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new DirectoryNotFoundException("Assembly directory is not found.");
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

        public static T? Return<T>(this ITinyMessengerHub messenger, ITinyReturn<T> msg)
        {
            messenger.Publish(msg);
            return msg.ResponseValue;
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

        /// <summary>
        /// Subscribe to a message type with the given destination and delivery action.
        /// All references are held with strong references
        ///
        /// All messages of this type will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="deliveryAction">Action to invoke when message is delivered</param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub, ITinyRecipient<TMessage> recipient) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive);
        }

        /// <summary>
        /// Subscribe to a message type with the given destination and delivery action.
        /// Messages will be delivered via the specified proxy.
        /// All references (apart from the proxy) are held with strong references
        ///
        /// All messages of this type will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="deliveryAction">Action to invoke when message is delivered</param>
        /// <param name="proxy">Proxy to use when delivering the messages</param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub, ITinyRecipient<TMessage> recipient, ITinyMessageProxy proxy) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive, proxy);
        }

        /// <summary>
        /// Subscribe to a message type with the given destination and delivery action.
        ///
        /// All messages of this type will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="deliveryAction">Action to invoke when message is delivered</param>
        /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub, ITinyRecipient<TMessage> recipient, bool useStrongReferences) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive, useStrongReferences);
        }

        /// <summary>
        /// Subscribe to a message type with the given destination and delivery action.
        /// Messages will be delivered via the specified proxy.
        ///
        /// All messages of this type will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="deliveryAction">Action to invoke when message is delivered</param>
        /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
        /// <param name="proxy">Proxy to use when delivering the messages</param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub, ITinyRecipient<TMessage> recipient, bool useStrongReferences, ITinyMessageProxy proxy) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive, useStrongReferences, proxy);
        }

        /// <summary>
        /// Subscribe to a message type with the given destination and delivery action with the given filter.
        /// All references are held with WeakReferences
        ///
        /// Only messages that "pass" the filter will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="deliveryAction">Action to invoke when message is delivered</param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub, ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive, messageFilter);
        }

        /// <summary>
        /// Subscribe to a message type with the given destination and delivery action with the given filter.
        /// Messages will be delivered via the specified proxy.
        /// All references (apart from the proxy) are held with WeakReferences
        ///
        /// Only messages that "pass" the filter will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="deliveryAction">Action to invoke when message is delivered</param>
        /// <param name="proxy">Proxy to use when delivering the messages</param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub, ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter, ITinyMessageProxy proxy) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive, messageFilter, proxy);
        }

        /// <summary>
        /// Subscribe to a message type with the given destination and delivery action with the given filter.
        /// All references are held with WeakReferences
        ///
        /// Only messages that "pass" the filter will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="deliveryAction">Action to invoke when message is delivered</param>
        /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub, ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter, bool useStrongReferences) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive, messageFilter, useStrongReferences);
        }

        /// <summary>
        /// Subscribe to a message type with the given destination and delivery action with the given filter.
        /// Messages will be delivered via the specified proxy.
        /// All references are held with WeakReferences
        ///
        /// Only messages that "pass" the filter will be delivered.
        /// </summary>
        /// <typeparam name="TMessage">Type of message</typeparam>
        /// <param name="deliveryAction">Action to invoke when message is delivered</param>
        /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
        /// <param name="proxy">Proxy to use when delivering the messages</param>
        /// <returns>TinyMessageSubscription used to unsubscribing</returns>
        public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub, ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter, bool useStrongReferences, ITinyMessageProxy proxy) where TMessage : class, ITinyMessage
        {
            return hub.Subscribe<TMessage>(recipient.Receive, messageFilter, useStrongReferences, proxy);
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

        public static void WriteNullTerminatedString(this BinaryWriter writer, string content)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(content);
            writer.Write(bytes);
            writer.Write((byte)0);
        }

        public static IEnumerable<Type> GetTypesWithAttribute<T>(Assembly? assembly = null) where T : Attribute
        {
            IEnumerable<Type> types;
            if (assembly == null)
                types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes());
            else
                types = assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.GetCustomAttributes(typeof(T), true).Length > 0)
                {
                    yield return type;
                }
            }
        }
        public static IEnumerable<(Type type, T attr)> GetTypesWithSingleAttribute<T>(Assembly? assembly = null) where T : Attribute
        {
            IEnumerable<Type> types;
            if (assembly == null)
                types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes());
            else
                types = assembly.GetTypes();
            foreach (Type type in types)
            {
                var attr = type.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;
                if (attr != null)
                {
                    yield return (type, attr);
                }
            }
        }
    }
}