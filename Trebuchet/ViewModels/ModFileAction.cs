using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;

namespace Trebuchet.ViewModels;

public class ModFileAction(string name, string icon, ICommand action, string classes = "Base") : 
    ReactiveObject
{
    public ICommand Action { get; } = action;
    public string Name { get; } = name;
    public string Icon { get; } = icon;
    public string Classes { get; set; } = classes;
}
