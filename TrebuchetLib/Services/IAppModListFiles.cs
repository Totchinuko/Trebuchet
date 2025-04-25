namespace TrebuchetLib.Services;

public interface IAppModListFiles : IAppFileHandler<ModListProfile, ModListProfileRef>, IAppFileHandlerWithImport<ModListProfile, ModListProfileRef>
{
    bool ResolveMod(ref string path);
    IEnumerable<string> ResolveMods(IEnumerable<string> modlist, bool throwIfFailed = true);
}