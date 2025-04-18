namespace TrebuchetLib;

public class ConsoleLogArgs
{
    public List<ConsoleLog> Logs { get; } = [];

    public ConsoleLogArgs Append(IEnumerable<ConsoleLog> logs)
    {
        Logs.AddRange(logs);
        return this;
    }
    public ConsoleLogArgs Append(ConsoleLog logs)
    {
        Logs.Add(logs);
        return this;
    }
}