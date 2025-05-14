using Microsoft.Extensions.Logging;

namespace TrebuchetLib.Sequences;

public class SequenceactionWaitOffline : ISequenceAction
{
    public bool CancelOnFailure { get; set; }
    public TimeSpan TimeOut { get; set; }
    public async Task Execute(SequenceArgs args)
    {
        var start = DateTime.UtcNow;
        while (args.Launcher.GetServerProcesses().Any(x => x.Infos.Instance == args.Instance) 
               && (DateTime.UtcNow - start) < TimeOut)
            await Task.Delay(25);
    }
}