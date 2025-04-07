using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Trebuchet.Controls;

public partial class ModFileAction : UserControl
{
    public Border StatusBorder => this.FindControl<Border>("Action")!;
    
    public ModFileAction()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is ViewModels.ModFileAction action)
        {
            StatusBorder.Classes.AddRange(action.Classes.Split(' '));
        }
    }
}