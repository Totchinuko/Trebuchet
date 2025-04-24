namespace TrebuchetLib.Services;

public interface IAppSyncFiles : IAppFileHandler<SyncProfile>, IAppFileHandlerWithImport<SyncProfile>
{
    IEnumerable<string> GetResolvedModlist(IEnumerable<string> modlist, bool throwIfFailed = true);
    Task Sync(string name);
}