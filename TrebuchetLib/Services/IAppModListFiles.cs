namespace TrebuchetLib.Services;

public interface IAppModListFiles : IAppFileHandler<ModListProfile>, IAppFileHandlerWithImport<ModListProfile>
{
    bool TryParseDirectory2ModId(string fullPath, out ulong id);
    bool ResolveMod(ref string path);
    IEnumerable<ulong> CollectAllMods(IEnumerable<string> modlists);
    IEnumerable<ulong> CollectAllMods(string modlist);
    IEnumerable<ulong> GetModIdList(IEnumerable<string> modlist);
    IEnumerable<string> ParseModList(IEnumerable<string> modlist);
    IEnumerable<string> GetResolvedModlist(IEnumerable<string> modlist);
}