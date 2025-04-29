using TrebuchetLib;

namespace Trebuchet.ViewModels;

public class FileViewModelEventArgs<T, TRef>(TRef reference, FileViewAction action)
    where T : ProfileFile<T>
    where TRef : class,IPRef<T, TRef>
{
    public TRef Reference { get; } = reference;
    public FileViewAction Action { get; } = action;
}