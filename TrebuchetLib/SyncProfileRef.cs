using System.Diagnostics.CodeAnalysis;
using TrebuchetLib.Services;

namespace TrebuchetLib;

public class SyncProfileRef(string name, AppSyncFiles handler) : 
    IPRef<SyncProfile, SyncProfileRef>,
    IPRefWithModList,
    IPRefWithClientConnection
{
    public string Name { get; } = name;
    public Uri Uri { get; } = new ($"{Constants.UriScheme}://{Constants.UriSyncHost}/{Uri.EscapeDataString(name)}");
    public AppSyncFiles Handler { get; } = handler;
    IAppFileHandler<SyncProfile, SyncProfileRef> IPRef<SyncProfile, SyncProfileRef>.Handler => Handler;

    public override string ToString()
    {
        return Name;
    }

    public bool TryGetModList([NotNullWhen(true)]out IEnumerable<string>? modList)
    {
        if (Handler.TryGet(this, out var profile))
        {
            modList = profile.Modlist;
            return true;
        }
        modList = null;
        return false;
    }

    IPRefWithModList IPRefWithModList.Resolve()
    {
        return Handler.Resolve(this);
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

    IPRefWithClientConnection IPRefWithClientConnection.Resolve()
    {
        return Handler.Resolve(this);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SyncProfileRef reference) return false;
        return reference.Name == Name;
    }
    
    protected bool Equals(SyncProfileRef other)
    {
        return Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}