using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goog
{
    public abstract class ConfigFile<T> where T : ConfigFile<T>
    {
        public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IgnoreReadOnlyProperties = true,
            TypeInfoResolver = new PrivateConstructorContractResolver()
        };

        private string _filePath = string.Empty;

        protected ConfigFile()
        { }

        public event EventHandler<T>? FileSaved;

        [JsonIgnore]
        public string FilePath { get => _filePath; private set => _filePath = value; }

        public static T CreateFile(string path)
        {
            T? file = (T?)Activator.CreateInstance(typeof(T));
            if (file == null)
                throw new Exception($"Failed to create data of type {typeof(T)}");
            file.FilePath = path;
            return file;
        }

        public static T LoadFile(string path)
        {
            if (!File.Exists(path))
                return CreateFile(path);
            string json = File.ReadAllText(path);
            T? file = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            if (file == null)
                throw new Exception($"{path} could not be loaded");
            file.FilePath = path;
            return file;
        }

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

            Tools.DeepCopy(dataFolder, folder);
        }

        public void DeleteFile()
        {
            if (!File.Exists(FilePath))
                throw new FileNotFoundException($"{FilePath} not found");
            File.Delete(FilePath);
        }

        public void DeleteFolder()
        {
            if (!File.Exists(FilePath))
                throw new FileNotFoundException($"{FilePath} not found");
            string? folder = Path.GetDirectoryName(FilePath);
            if (folder == null || !Directory.Exists(folder))
                throw new DirectoryNotFoundException($"Invalid directory for {FilePath}");

            Directory.Delete(folder, true);
        }

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

        protected virtual void OnFileSaved()
        {
            FileSaved?.Invoke(this, (T)this);
        }
    }
}