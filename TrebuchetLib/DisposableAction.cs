namespace TrebuchetLib;

public class DisposableAction(Action action) : IDisposable
{
    public void Dispose()
    {
        action.Invoke();
    }
}