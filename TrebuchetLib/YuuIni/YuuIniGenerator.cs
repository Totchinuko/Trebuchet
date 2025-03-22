using TrebuchetLib.Processes;
using TrebuchetLib.Services;

namespace TrebuchetLib.YuuIni;

public class YuuIniGenerator(AppClientFiles clientFiles, AppServerFiles serverFiles) : IIniGenerator
{
    public async Task WriteClientSettingsAsync(ClientProfile profile)
    {
        await new YuuIniClientFiles(clientFiles).WriteIni(profile);
    }

    public async Task WriteServerSettingsAsync(ServerProfile profile, int instance)
    {
        await new YuuIniServerFiles(serverFiles).WriteIni(profile, instance);   
    }

    public Task<ConanServerInfos> GetInfosFromServerAsync(int instance)
    {
        return new YuuIniServerFiles(serverFiles).GetInfosFromIni(instance);
    }
}