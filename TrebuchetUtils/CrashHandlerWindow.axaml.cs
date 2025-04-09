using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TrebuchetUtils;

public partial class CrashHandlerWindow : Window
{
    public CrashHandlerWindow()
    {
        InitializeComponent();
    }

    private void Exit_Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}