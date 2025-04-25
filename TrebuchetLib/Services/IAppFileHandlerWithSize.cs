namespace TrebuchetLib.Services;

public interface IAppFileHandlerWithSize<T, TRef>  where T : JsonFile<T> where TRef : IPRef<T, TRef>
{
    Task<long> GetSize(TRef name);
}