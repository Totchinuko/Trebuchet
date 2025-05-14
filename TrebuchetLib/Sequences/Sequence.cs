namespace TrebuchetLib.Sequences;

public class Sequence
{
    public List<ISequenceAction> Actions { get; set; } = [];

    public async Task ExecuteSequence(SequenceArgs args, IProgress<double> progress)
    {
        int i = 0;
        progress.Report(0);
        foreach (var action in Actions)
        {
            if (args.CancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();
            await action.Execute(args);
            i++;
            progress.Report((double)i / Actions.Count);
        }
    }
}