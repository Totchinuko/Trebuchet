using TrebuchetLib;

namespace Trebuchet.ViewModels;

public class FileViewModelEventArgs<T, TRef>(TRef reference, FileViewAction action)
    where T : JsonFile<T>
    where TRef : IPRef<T, TRef>
{
    public TRef Reference { get; } = reference;
    public FileViewAction Action { get; } = action;
}