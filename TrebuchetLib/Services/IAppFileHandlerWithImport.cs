namespace TrebuchetLib.Services;

public interface IAppFileHandlerWithImport<T> where T : JsonFile<T>
{
    Task Export(string name, FileInfo exportFile);
    Task<T> Import(FileInfo importFile, string name);
    Task<T> Import(string json, string name);
}