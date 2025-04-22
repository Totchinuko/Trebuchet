namespace TrebuchetLib.Services;

public interface IAppFileHandlerWithImport<T> where T : JsonFile<T>
{
    Task Export(FileInfo exportFile);
    Task<T> Import(FileInfo importFile);
}