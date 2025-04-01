using Avalonia.Media;

namespace TrebuchetUtils;

public interface IApplication
{
    IImage? AppIconPath { get; }
    bool HasCrashed { get; }

    void Crash();
}