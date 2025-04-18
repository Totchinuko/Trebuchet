namespace TrebuchetLib;

public class LogEventArgs : EventArgs
{
    public List<LogEventLine> Lines { get; } = [];

    public LogEventArgs Append(params LogEventLine[] lines)
    {
        Lines.AddRange(lines);
        return this;
    }

    public LogEventArgs Append(IEnumerable<LogEventLine> lines)
    {
        Lines.AddRange(lines);
        return this;
    }
}