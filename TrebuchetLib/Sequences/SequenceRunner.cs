namespace TrebuchetLib.Sequences;

public class SequenceRunner
{
    public SequenceRunner(Sequence sequence, SequenceArgs args, IProgress<SequenceProgress> progress)
    {
        _sequence = sequence;
        Arguments = args;
        _progress = progress;

        Arguments.CancellationToken = Cts.Token;
    }
    
    private readonly Sequence _sequence;
    private readonly IProgress<SequenceProgress> _progress;
    
    public SequenceArgs Arguments { get; }
    public CancellationTokenSource Cts { get; } = new();

    public async Task ExecuteSequence()
    {
        int i = 0;
        _progress.Report(new SequenceProgress(Arguments.Instance, 0, _sequence.Actions.Count));
        foreach (var action in _sequence.Actions.ToList())
        {
            if (Arguments.CancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();
            await action.Execute(Arguments);
            i++;
            _progress.Report(new SequenceProgress(Arguments.Instance, i, _sequence.Actions.Count));
        }
        _progress.Report(new SequenceProgress(Arguments.Instance, 0, 0));
    }
}