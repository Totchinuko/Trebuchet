using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TrebuchetLib.Services;

public class Progress : IProgress<double>
{
    private double _value;
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    
    public event EventHandler<double>? ProgressChanged;

    public async void Report(double value)
    {
        await _semaphore.WaitAsync();
        _value = value;
        ProgressChanged?.Invoke(this, value);
        _semaphore.Release();
    }

    public async Task<double> GetProgressAsync()
    {
        await _semaphore.WaitAsync();
        var value = _value;
        _semaphore.Release();
        return value;
    }
}