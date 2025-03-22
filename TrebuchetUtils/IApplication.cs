namespace TrebuchetUtils;

public interface IApplication
{
    string AppIconPath { get; }
    bool HasCrashed { get; }

    void Crash();
}