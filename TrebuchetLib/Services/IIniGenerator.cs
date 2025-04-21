using TrebuchetLib.Processes;

namespace TrebuchetLib.Services;

public interface IIniGenerator
{
    Task WriteClientSettingsAsync(ClientProfile profile);
    Task WriteClientLastConnection(string address, int port, string password);
    Task WriteServerSettingsAsync(ServerProfile profile, int instance);
    Task<ConanServerInfos> GetInfosFromServerAsync(int instance);
}