namespace TrebuchetLib.Services;

public interface IAppServerFiles : IAppFileHandler<ServerProfile>
{
    string GetGameLogs(string name);
}