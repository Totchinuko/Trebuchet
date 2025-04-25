using TrebuchetLib.Services;

namespace TrebuchetLib;

public class ServerProfileRef(string name, IAppFileHandler<ServerProfile, ServerProfileRef> handler) : IPRef<ServerProfile, ServerProfileRef>
{
    public string Name { get; } = name;
    public Uri Uri { get; } = new ($"{Constants.UriScheme}://{Constants.UriServerHost}/{Uri.EscapeDataString(name)}");
    public IAppFileHandler<ServerProfile, ServerProfileRef> Handler { get; } = handler;

    public override string ToString()
    {
        return Name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ServerProfileRef reference) return false;
        return reference.Name == Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}