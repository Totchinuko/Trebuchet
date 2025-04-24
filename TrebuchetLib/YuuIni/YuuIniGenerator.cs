using TrebuchetLib.Processes;
using TrebuchetLib.Services;

namespace TrebuchetLib.YuuIni;

public class YuuIniGenerator(AppSetup setup) : IIniGenerator
{
    public async Task WriteClientSettingsAsync(ClientProfile profile)
    {
        await new YuuIniClientFiles(setup).WriteIni(profile);
    }

    public async Task WriteClientLastConnection(ClientConnection connection)
    {
        await new YuuIniClientFiles(setup).WriteLastConnection(connection);
    }

    public async Task WriteServerSettingsAsync(ServerProfile profile, int instance)
    {
        await new YuuIniServerFiles(setup).WriteIni(profile, instance);   
    }

    public Task<ConanServerInfos> GetInfosFromServerAsync(int instance)
    {
        return new YuuIniServerFiles(setup).GetInfosFromIni(instance);
    }
}