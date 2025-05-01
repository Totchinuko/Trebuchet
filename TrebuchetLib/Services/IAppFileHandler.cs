using System.Diagnostics.CodeAnalysis;

namespace TrebuchetLib.Services;

public interface IAppFileHandler<T, TRef> 
    where T : ProfileFile<T> 
    where TRef : class,IPRef<T, TRef>
{
    bool UseSubFolders { get; }
    Dictionary<TRef, T> Cache { get; }
    string GetBaseFolder();
    TRef Ref(string name);
}