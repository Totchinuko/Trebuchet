using Avalonia.Controls;
using Trebuchet.Assets;
using TrebuchetLib;

namespace Trebuchet.ViewModels;

public class ModListRefViewModel(IPRefWithModList modList)
{
    public IPRefWithModList ModList { get; } = modList;

    private static string DefineLabel(IPRefWithModList modList)
    {
        if (modList is ModListProfileRef)
            return $@"{Resources.PanelMods}: {modList.Name}";
        if (modList is SyncProfileRef)
            return $@"{Resources.Sync}: {modList.Name}";
        return modList.Name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ModListRefViewModel vm) return false;
        return vm.ModList.Equals(ModList);
    }

    protected bool Equals(ModListRefViewModel other)
    {
        return ModList.Equals(other.ModList);
    }

    public override int GetHashCode()
    {
        return ModList.GetHashCode();
    }

    public override string ToString()
    {
        return DefineLabel(ModList);
    }
}