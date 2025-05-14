using Microsoft.Extensions.Logging;

namespace TrebuchetLib.Sequences;

public class SequenceActionBackupServerData : ISequenceAction
{
    public bool CancelOnFailure { get; set; }
    public TimeSpan MaxAge { get; set; }
    
    public async Task Execute(SequenceArgs args)
    {
        if (args.Launcher.GetServerProcesses().Any(x => x.Infos.Instance == args.Instance))
        {
            if (CancelOnFailure)
                throw new OperationCanceledException("Cannot backup while server is running");
            args.Logger.LogError("Cannot backup while server is running");
            return;
        }

        try
        {
            await args.BackupManager.PerformServerBackup(args.Instance, MaxAge);
        }
        catch (Exception ex)
        {
            if (CancelOnFailure)
                throw new OperationCanceledException("Failed to perform sequence backup", ex);
            args.Logger.LogError(ex, "Failed to perform sequence backup");
        }
    }
}