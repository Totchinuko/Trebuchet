using TrebuchetLib.Processes;

namespace TrebuchetLib.Services;

public interface IIniGenerator
{
    Task WriteClientSettingsAsync(ClientProfile profile);
    Task WriteClientLastConnection(ClientConnection connection);
    Task WriteServerSettingsAsync(ServerProfile profile, int instance);
    Task<ConanServerInfos> GetInfosFromServerAsync(int instance);
}