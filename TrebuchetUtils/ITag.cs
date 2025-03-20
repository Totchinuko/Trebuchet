using System.Windows.Input;
using Avalonia.Media;

namespace TrebuchetUtils
{
    public interface ITag
    {
        SolidColorBrush Color { get; }
        string Name { get; }
    }
}