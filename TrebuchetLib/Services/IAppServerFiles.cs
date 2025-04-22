namespace TrebuchetLib.Services;

public interface IAppServerFiles : IAppFileHandler<ServerProfile>, IAppFileHandlerWithSize<ServerProfile>
{
    string GetGameLogs(string name);
}