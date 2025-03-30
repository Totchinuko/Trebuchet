namespace TrebuchetLib.Services;

public interface IProgressCallback<T> : IProgress<T>
{
    event EventHandler<T>? ProgressChanged;
    Task<T> GetProgressAsync();
}