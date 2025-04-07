using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Trebuchet.ViewModels;

namespace Trebuchet.Controls;

public partial class ModFile : UserControl
{
    public Border StatusBorder => this.FindControl<Border>("Status")!;
    public Border IconBorder => this.FindControl<Border>("Icon")!;
    
    public ModFile()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is IModFile modfile)
        {
            StatusBorder.Classes.AddRange(modfile.StatusClasses.Split(' '));
            IconBorder.Classes.AddRange(modfile.IconClasses.Split(' '));
        }
    }
}