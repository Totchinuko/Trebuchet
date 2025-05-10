using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using ReactiveUI;
using tot_gui_lib;

namespace Trebuchet.ViewModels;

public class ModFileAction(string name, [Localizable(false)] string icon, ICommand action) : 
    ReactiveObject
{
    public ICommand Action { get; } = action;
    public string Name { get; } = name;
    public string Icon { get; } = icon;
    public ObservableCollection<string> Classes { get; } = [@"Base"];
}
