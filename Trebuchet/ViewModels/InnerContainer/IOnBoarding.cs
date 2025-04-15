using Avalonia.Layout;

namespace Trebuchet.ViewModels.InnerContainer;

public interface IOnBoarding
{
    HorizontalAlignment HorizontalAlignment { get; }
    VerticalAlignment VerticalAlignment { get; }
    int Width { get; }
    int Height { get; }
}