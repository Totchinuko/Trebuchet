using Microsoft.Extensions.Logging;

namespace TrebuchetLib.Sequences;

public class SequenceActionWaitOnline : ISequenceAction
{
    public bool CancelOnFailure { get; set; }
    public TimeSpan TimeOut { get; set; }
    public async Task Execute(SequenceArgs args)
    {
        var start = DateTime.UtcNow;
        var process = args.Launcher.GetServerProcesses().FirstOrDefault(x => x.Infos.Instance == args.Instance);
        if (process is null)
        {
            if (CancelOnFailure)
                throw new OperationCanceledException("Server is not launched");
            args.Logger.LogError("Server is not launched");
            return;
        }

        while (process.State != ProcessState.ONLINE && (DateTime.UtcNow - start) < TimeOut)
            await Task.Delay(25);
    }
}