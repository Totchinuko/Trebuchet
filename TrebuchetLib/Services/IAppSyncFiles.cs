namespace TrebuchetLib.Services;

public interface IAppSyncFiles : IAppFileHandler<SyncProfile, SyncProfileRef>
    
{
    IEnumerable<string> GetResolvedModlist(IEnumerable<string> modlist, bool throwIfFailed = true);
    Task Sync(SyncProfileRef name);
}