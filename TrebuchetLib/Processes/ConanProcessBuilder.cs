using System.Diagnostics;
using System.Net;
using TrebuchetLib.Services;

namespace TrebuchetLib.Processes;

public class ConanProcessBuilder
{
    private ConanProcessBuilder()
    {
    }
    
    private DateTime _start = DateTime.UtcNow;
    private FileInfo? _log;
    private ConanServerInfos? _serverInfos;
    private Process? _process;
    private bool _logAtBeginning;

    public static ConanProcessBuilder Create()
    {
        return new ConanProcessBuilder();
    }

    public ConanProcessBuilder SetStartingTime(DateTime time)
    {
        _start = time;
        return this;
    }

    public ConanProcessBuilder SetLogFile(string logFile)
    {
        _log = new FileInfo(logFile);
        return this;
    }

    public ConanProcessBuilder StartLogAtBeginning()
    {
        _logAtBeginning = true;
        return this;
    }

    public ConanProcessBuilder SetServerInfos(ServerProfile profile, int instance)
    {
        _serverInfos = new ConanServerInfos(profile, instance);
        return this;
    }

    public async Task<ConanProcessBuilder> SetServerInfos(IIniGenerator iniGenerator, int instance)
    {
        _serverInfos = await iniGenerator.GetInfosFromServerAsync(instance);
        return this;
    }

    public ConanProcessBuilder SetProcess(Process process)
    {
        _process = process;
        return this;
    }

    public IConanServerProcess BuildServer()
    {
        if (_process is null) throw new ArgumentNullException(nameof(_process), @"Process is not set");
        if(_serverInfos is null) throw new ArgumentNullException(nameof(_serverInfos), @"ServerInfos is not set");
        if(_log is null) throw new ArgumentNullException(nameof(_log), @"Log is not set");
        
        var logReader = new LogReader(_log.FullName);
        if(_logAtBeginning) logReader.StartAtBeginning();
        else logReader.Start();
        
        var sourceQuery = new SourceQueryReader(new IPEndPoint(IPAddress.Loopback, _serverInfos.QueryPort), 4 * 1000, 5 * 1000);
        sourceQuery.StartQueryThread();
        
        var rcon = new Rcon(new IPEndPoint(IPAddress.Loopback, _serverInfos.RConPort), _serverInfos.RConPassword, timeout:10, keepAlive:300);
        var console = new MixedConsole(rcon).AddLogSource(logReader, ConsoleLogSource.ServerLog);

        return new ConanServerProcess(_process)
        {
            SourceQueryReader = sourceQuery,
            LogReader = logReader,
            StartUtc = _start,
            RCon = rcon,
            Console = console,
            ServerInfos = _serverInfos
        };
    }

    public IConanProcess BuildClient()
    {
        if (_process is null) throw new ArgumentNullException(nameof(_process), @"Process is not set");

        return new ConanClientProcess(_process)
        {
            StartUtc = _start
        };
    }
}