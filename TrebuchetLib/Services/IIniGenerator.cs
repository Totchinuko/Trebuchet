using TrebuchetLib.Processes;

namespace TrebuchetLib.Services;

public interface IIniGenerator
{
    Task WriteClientSettingsAsync(ClientProfile profile);
    
    Task WriteServerSettingsAsync(ServerProfile profile, int instance);

    Task<ConanServerInfos> GetInfosFromServerAsync(int instance);
}