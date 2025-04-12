using Avalonia;
using Avalonia.Controls;

namespace Trebuchet.Controls;

public partial class FieldRowControl : UserControl
{
    public static readonly StyledProperty<object?> FieldContentProperty
        = AvaloniaProperty.Register<FieldRowControl, object?>(nameof(FieldContent));
    public FieldRowControl()
    {
        InitializeComponent();
    }

    public object? FieldContent
    {
        get => GetValue(FieldContentProperty);
        set => SetValue(FieldContentProperty, value);
    }
}