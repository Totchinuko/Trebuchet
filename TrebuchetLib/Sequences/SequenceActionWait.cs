namespace TrebuchetLib.Sequences;

public class SequenceActionWait : ISequenceAction
{
    public TimeSpan WaitTime { get; set; }
    
    public Task Execute(SequenceArgs args)
    {
        return Task.Delay(WaitTime);
    }
}