namespace Trebuchet.ViewModels;

public class FileViewModelEventArgs(string name, FileViewAction action)
{
    public string Name { get; } = name;
    public FileViewAction Action { get; } = action;
}