namespace TrebuchetLib.Sequences;

public class SequenceMainAction : ISequenceAction
{
    public Task Execute(SequenceArgs args)
    {
        return args.MainAction.Invoke();
    }
}