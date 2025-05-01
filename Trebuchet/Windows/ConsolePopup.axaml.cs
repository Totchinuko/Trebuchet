using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Trebuchet.ViewModels;

namespace Trebuchet.Windows;

public partial class ConsolePopup : Window
{
    public ConsolePopup()
    {
        InitializeComponent();
        PopupOut.Closed += (_, _) => Close();
    }

    public MixedConsolePopedOutViewModel PopupOut { get; } = new();
    public required int Instance { get; init; }
}