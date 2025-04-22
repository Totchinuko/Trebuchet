using System.Diagnostics.CodeAnalysis;

namespace TrebuchetLib.Services;

public interface IAppFileHandler<T> where T : JsonFile<T>
{
    T Create(string name);
    T Get(string name);
    bool TryGet(string name, [NotNullWhen(true)] out T? file);
    bool Exists(string name);
    void Delete(string name);
    Task<T> Duplicate(string name, string created);
    Task<T> Rename(string name, string changed);
    Task<long> GetSize(string name);
    IEnumerable<string> GetList();
    string GetDefault();
    string GetBaseFolder();
    string GetPath(string name);
}