using TrebuchetUtils;

namespace Trebuchet.Messages;

public class ModListMessages(object? sender) : ITinyMessage
{
    public object? Sender { get; } = sender;
}

public class ModListModFileMessage(object? sender, ModFile modFile) : ModListMessages(sender)
{
    public ModFile ModFile { get; } = modFile;
}

public class ModListOpenModSteamMessage(object? sender, ModFile modFile) : ModListModFileMessage(sender, modFile) {}

public class ModListRemoveModMessage(object? sender, ModFile modFile) : ModListModFileMessage(sender, modFile) {}

public class ModListUpdateModMessage(object? sender, ModFile modFile) : ModListModFileMessage(sender, modFile) {}