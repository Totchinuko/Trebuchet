using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrebuchetLib
{
    public abstract class JsonFile<T> where T : JsonFile<T>
    {
        public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true,
            TypeInfoResolver = new PrivateConstructorContractResolver()
        };

        public event EventHandler<T>? FileSaved;

        [JsonIgnore]
        public string FilePath { get; protected set; } = string.Empty;

        /// <summary>
        /// Copy the file to the specified path
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        public void CopyFileTo(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is invalid");

            string? folder = Path.GetDirectoryName(path);
            if (folder == null)
                throw new DirectoryNotFoundException($"Invalid directory for {path}");

            if (File.Exists(path))
                throw new Exception($"{path} already exists");

            string? dataFolder = Path.GetDirectoryName(FilePath);
            if (dataFolder == null)
                throw new DirectoryNotFoundException($"Invalid directory for {FilePath}");

            File.Copy(FilePath, path);
        }

        /// <summary>
        /// Copy the file and its parent folder to the specified path
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        public void CopyFolderTo(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is invalid");

            string? folder = Path.GetDirectoryName(path);
            if (folder == null)
                throw new DirectoryNotFoundException($"Invalid directory for {path}");

            if (File.Exists(path) || (Directory.Exists(folder)))
                throw new Exception($"{path} already exists");

            string? dataFolder = Path.GetDirectoryName(FilePath);
            if (dataFolder == null)
                throw new DirectoryNotFoundException($"Invalid directory for {FilePath}");

            Tools.CreateDir(folder);
            Tools.DeepCopy(dataFolder, folder);
        }

        /// <summary>
        /// Delete the file
        /// </summary>
        /// <exception cref="FileNotFoundException"></exception>
        public void DeleteFile()
        {
            if (!File.Exists(FilePath))
                throw new FileNotFoundException($"{FilePath} not found");
            File.Delete(FilePath);
        }

        /// <summary>
        /// Delete file and its parent folder
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void DeleteFolder()
        {
            string? folder = Path.GetDirectoryName(FilePath);
            if (folder == null || !Directory.Exists(folder))
                throw new DirectoryNotFoundException($"Invalid directory for {FilePath}");

            Directory.Delete(folder, true);
        }

        /// <summary>
        /// Move the file and its parent folder to the specified path
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        public void MoveFolderTo(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is invalid");

            string? targetFolder = Path.GetDirectoryName(path);
            if (targetFolder == null)
                throw new DirectoryNotFoundException($"Invalid directory for {path}");

            if (File.Exists(path) || Directory.Exists(targetFolder))
                throw new Exception($"{path} already exists");

            string? folder = Path.GetDirectoryName(FilePath);
            if (folder == null)
                throw new DirectoryNotFoundException($"Invalid directory for {FilePath}");

            Directory.Move(folder, targetFolder);
            FilePath = path;
        }

        /// <summary>
        /// Save the file to disk as Json file
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void SaveFile()
        {
            string json = JsonSerializer.Serialize(this, typeof(T), _jsonOptions);
            string? folder = Path.GetDirectoryName(FilePath);
            if (folder == null)
                throw new Exception($"{FilePath} is an invalid path");
            Tools.CreateDir(folder);
            File.WriteAllText(FilePath, json);
            OnFileSaved();
        }

        /// <summary>
        /// Create a new file to be saved at the specified path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="constructor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T CreateFile(string path, params object[] constructor)
        {
            T? file = (T?)Activator.CreateInstance(typeof(T), constructor);
            if (file == null)
                throw new Exception($"Failed to create data of type {typeof(T)}");
            file.FilePath = path;
            return file;
        }

        /// <summary>
        /// Load the file from json at the specified path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected static T LoadFile(string path, JsonSerializerOptions? options = null)
        {
            if (!File.Exists(path))
                return CreateFile(path);
            string json = File.ReadAllText(path);
            T? file = JsonSerializer.Deserialize<T>(json, options != null ? options : _jsonOptions);
            if (file == null)
                throw new Exception($"{path} could not be loaded");
            file.FilePath = path;
            return file;
        }

        protected virtual void OnFileSaved()
        {
            FileSaved?.Invoke(this, (T)this);
        }
    }
}