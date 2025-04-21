using System.Diagnostics;
using TrebuchetLib.Services;

namespace TrebuchetLib.Processes;

public interface IConanProcessBuilder
{
    IConanProcessBuilderWithProcess SetProcess(Process process);
    IConanProcessBuilder SetStartDate(DateTime date);
}

public interface IConanProcessBuilderWithProcess : IConanProcessBuilder
{
    Task<IConanProcess> BuildClient();
    IConanProcessServerBuilder SetServerInfos(ServerProfile profile, int instance);
    IConanProcessServerBuilder SetServerInfos(IIniGenerator iniGenerator, int instance);
    IConanProcessServerBuilder SetServerInfos(ConanServerInfos infos);
}


public interface IConanProcessServerBuilder : IConanProcessBuilderWithProcess
{
    IConanProcessServerBuilderLogTracked SetLogFile(string logFile);
}

public interface IConanProcessServerBuilderLogTracked : IConanProcessServerBuilder
{
    IConanProcessServerBuilderLogTracked StartLogAtBeginning();
    Task<IConanServerProcess> BuildServer();
}