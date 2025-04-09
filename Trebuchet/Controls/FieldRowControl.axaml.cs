using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace Trebuchet.Controls;

public partial class FieldRowControl : UserControl
{
    public readonly static StyledProperty<object?> FieldContentProperty
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