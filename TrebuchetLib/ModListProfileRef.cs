using System.Diagnostics.CodeAnalysis;
using TrebuchetLib.Services;

namespace TrebuchetLib;

public class ModListProfileRef(string name, IAppFileHandler<ModListProfile, ModListProfileRef> handler) : 
    IPRef<ModListProfile, ModListProfileRef>,
    IPRefWithModList
{
    public string Name { get; } = name;
    public Uri Uri { get; } = new ($"{Constants.UriScheme}://{Constants.UriModListHost}/{Uri.EscapeDataString(name)}");
    public IAppFileHandler<ModListProfile, ModListProfileRef> Handler { get; } = handler;

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

    public IPRefWithModList Resolve()
    {
        return Handler.Resolve(this);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ModListProfileRef reference) return false;
        return reference.Name == Name;
    }
    
    protected bool Equals(ModListProfileRef other)
    {
        return Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}