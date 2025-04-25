namespace TrebuchetLib.Services;

public interface IAppServerFiles : IAppFileHandler<ServerProfile, ServerProfileRef>, IAppFileHandlerWithSize<ServerProfile, ServerProfileRef>
{
    string GetGameLogs(ServerProfileRef name);
}