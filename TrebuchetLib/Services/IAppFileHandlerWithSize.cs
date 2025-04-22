namespace TrebuchetLib.Services;

public interface IAppFileHandlerWithSize<T>  where T : JsonFile<T>
{
    Task<long> GetSize(string name);
}