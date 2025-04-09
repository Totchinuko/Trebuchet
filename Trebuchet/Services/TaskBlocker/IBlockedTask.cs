using System.Threading;

namespace Trebuchet.Services.TaskBlocker;

public interface IBlockedTask
{
    CancellationTokenSource Cts { get; }
    void Release();
}