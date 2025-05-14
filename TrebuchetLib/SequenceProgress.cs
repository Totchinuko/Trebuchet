namespace TrebuchetLib;

public struct SequenceProgress(int instance, int current, int total)
{
    public int Instance { get; } = instance;
    public int Current { get; } = current;
    public int Total { get; } = total;
}