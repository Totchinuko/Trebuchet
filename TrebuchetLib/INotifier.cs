namespace TrebuchetLib;

public interface INotifier
{
    Task Notify(string message);
}