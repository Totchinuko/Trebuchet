using System.Diagnostics.CodeAnalysis;

namespace TrebuchetLib;

public class ClientConnectionRef(IPRefWithClientConnection source, string connection)
{
    public IPRefWithClientConnection Source { get; } = source;
    public string Connection { get; } = connection;

    public bool TryGet([NotNullWhen(true)] out ClientConnection? connection)
    {
        return Source.TryGetConnection(Connection, out connection);
    }
}