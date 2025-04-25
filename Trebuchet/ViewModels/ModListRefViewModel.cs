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

    public override string ToString()
    {
        return DefineLabel(ModList);
    }
}