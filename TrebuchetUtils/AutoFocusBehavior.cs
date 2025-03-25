using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace TrebuchetUtils;

public class AutoFocusBehavior : StyledElementBehavior<Control>
{
    protected override void OnLoaded()
    {
        base.OnLoaded();
        AssociatedObject?.Focus();
    }
}