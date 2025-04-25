using System.Diagnostics.CodeAnalysis;

namespace TrebuchetLib.Services;

public interface IAppFileHandler<T, TRef> where T : JsonFile<T> where TRef : IPRef<T, TRef>
{
    TRef Ref(string name);
    T Create(TRef name);
    T Get(TRef name);
    bool Exists(string name);
    bool Exists(TRef name);
    void Delete(TRef name);
    Task<T> Duplicate(TRef name, TRef created);
    Task<T> Rename(TRef name, TRef changed);
    IEnumerable<TRef> GetList();
    TRef GetDefault();
    string GetBaseFolder();
    string GetPath(TRef name);
}