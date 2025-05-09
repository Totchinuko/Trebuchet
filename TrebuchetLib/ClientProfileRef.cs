using System.Diagnostics.CodeAnalysis;
using System.Web;
using TrebuchetLib.Services;

namespace TrebuchetLib;

public class ClientProfileRef(string name, AppClientFiles handler) : 
    IPRef<ClientProfile, ClientProfileRef>,
    IPRefWithClientConnection
{
    public string Name { get; } = name;
    public Uri Uri { get; } = new ($"{Constants.UriScheme}://{Constants.UriClientHost}/{Uri.EscapeDataString(name)}", UriKind.Absolute);
    public AppClientFiles Handler { get; } = handler;
    IAppFileHandler<ClientProfile, ClientProfileRef> IPRef<ClientProfile, ClientProfileRef>.Handler => Handler;

    public override string ToString()
    {
        return Name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ClientProfileRef reference) return false;
        return reference.Name == Name;
    }

    protected bool Equals(ClientProfileRef other)
    {
        return Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
    
    public bool TryGetConnection(string name, [NotNullWhen(true)] out ClientConnection? connection)
    {
        connection = Handler.Get(this).ClientConnections.FirstOrDefault(x => x.Name == name);
        return connection is not null;
    }

    public IEnumerable<ClientConnection> GetConnections()
    {
        return Handler.Get(this).ClientConnections;
    }

    public IPRefWithClientConnection Resolve()
    {
        return Handler.Resolve(this);
    }
}