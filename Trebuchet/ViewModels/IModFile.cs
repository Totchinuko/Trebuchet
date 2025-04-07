using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using Trebuchet.ViewModels.Panels;

namespace Trebuchet.ViewModels;

public delegate void IModFileArgs(IModFile file);
public interface IModFile
{
    string Title { get; }
    string StatusClasses { get; }
    string IconClasses { get; }
    string LastUpdate { get; }
    string FilePath { get; }
    ObservableCollection<ModFileAction> Actions { get; }
    string Export();
}