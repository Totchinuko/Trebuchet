using TrebuchetLib.Processes;
using TrebuchetLib.Services;

namespace TrebuchetLib.YuuIni;

public class YuuIniGenerator(AppFiles appFiles, AppSetup setup) : IIniGenerator
{
    public async Task WriteClientSettingsAsync(ClientProfile profile)
    {
        await new YuuIniClientFiles(appFiles, setup).WriteIni(profile);
    }

    public async Task WriteServerSettingsAsync(ServerProfile profile, int instance)
    {
        await new YuuIniServerFiles(appFiles).WriteIni(profile, instance);   
    }

    public Task<ConanServerInfos> GetInfosFromServerAsync(int instance)
    {
        return new YuuIniServerFiles(appFiles).GetInfosFromIni(instance);
    }
}