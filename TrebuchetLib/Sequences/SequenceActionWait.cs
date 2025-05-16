namespace TrebuchetLib.Sequences;

public class SequenceActionWait : ISequenceAction
{
    public TimeSpan WaitTime { get; set; } = TimeSpan.FromMinutes(5);
    
    public Task Execute(SequenceArgs args)
    {
        return Task.Delay(WaitTime);
    }
}