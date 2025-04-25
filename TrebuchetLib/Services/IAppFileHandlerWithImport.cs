namespace TrebuchetLib.Services;

public interface IAppFileHandlerWithImport<T, TRef> where T : JsonFile<T> where TRef : IPRef<T, TRef>
{
    Task Export(TRef name, FileInfo exportFile);
    Task<T> Import(FileInfo importFile, TRef name);
    Task<T> Import(string json, TRef name);
}