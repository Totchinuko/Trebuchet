using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TrebuchetLib.Services;

namespace TrebuchetLib;

public interface IPRef
{
    string Name { get; }
    Uri Uri { get; }
}

public interface IPRef<T, TRef> : IPRef 
    where T : JsonFile<T> 
    where TRef : IPRef<T, TRef>
{
    IAppFileHandler<T, TRef> Handler { get; }
}

public interface IPRefWithModList
{
    string Name { get; }
    Uri Uri { get; }
    bool TryGetModList([NotNullWhen(true)]out IEnumerable<string>? modList);
    IPRefWithModList Resolve();
}

public interface IPRefWithClientConnection
{
    string Name { get; }
    Uri Uri { get; }
    bool TryGetConnection(string name, [NotNullWhen(true)]out ClientConnection? connection);
    IEnumerable<ClientConnection> GetConnections();
    IPRefWithClientConnection Resolve();
}