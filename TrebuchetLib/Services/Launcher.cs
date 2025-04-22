using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using tot_lib;
using TrebuchetLib.Processes;

namespace TrebuchetLib.Services;

public class Launcher(
    AppFiles appFiles, 
    AppSetup setup, 
    IIniGenerator iniHandler, 
    ConanProcessFactory processFactory,
    ILogger<Launcher> logger)
    : IDisposable
{
    private readonly Dictionary<int, IConanServerProcess> _serverProcesses = [];
    private IConanProcess? _conanClientProcess;
    private bool _hasCatapulted;

    public void Dispose()
    {
        _conanClientProcess?.Dispose();
        foreach (var item in _serverProcesses)
            item.Value.Dispose();
        _serverProcesses.Clear();
    }

    public async Task CatapultClient(bool isBattleEye, bool autoConnect)
    {
        var profile = setup.Config.SelectedClientProfile;
        var modlist = setup.Config.SelectedClientModlist;
        await CatapultClient(profile, modlist, isBattleEye, autoConnect);
    }

    public async Task<Process> CatapultClientProcess(bool isBattleEye, bool autoConnect)
    {
        var profile = setup.Config.SelectedClientProfile;
        var modlist = setup.Config.SelectedClientModlist;
        return await CatapultClientProcess(profile, modlist, isBattleEye, autoConnect);
    }

    /// <summary>
    ///     Launch a client process while taking care of everything. Generate the modlist, generate the ini settings, etc.
    ///     Process is created on a separate thread, and fire the event ClientProcessStarted when the process is running.
    /// </summary>
    /// <param name="profileName"></param>
    /// <param name="modlistName"></param>
    /// <param name="isBattleEye">Launch with BattlEye anti cheat.</param>
    /// <param name="autoConnect">Launch and try to connect to a server automatically</param>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="ArgumentException">
    ///     Profiles can only be used by one process at a times, since they contain the db of
    ///     the game.
    /// </exception>
    public async Task CatapultClient(string profileName, string modlistName, bool isBattleEye, bool autoConnect)
    {
        if (_conanClientProcess != null) return;

        var process = await CatapultClientProcess(profileName, modlistName, isBattleEye, autoConnect);

        _conanClientProcess = await processFactory.Create().SetProcess(process).BuildClient();
    }

    public async Task<Process> CatapultClientProcess(string profileName, string modlistName, bool isBattleEye, bool autoConnect)
    {
        var data = new Dictionary<string, object>
        {
            {@"profile", profileName},
            {"modlist", modlistName},
            {"isBattleEye", isBattleEye},
            {"autoConnect", autoConnect}
        };
        using var scope = logger.BeginScope(data);
        logger.LogInformation("Launching");
        
        if (!appFiles.Client.TryGet(profileName, out var profile))
            throw new TrebException($"{profileName} profile not found.");
        if (!appFiles.Mods.TryGet(modlistName, out var modlist))
            throw new TrebException($"{modlistName} modlist not found.");
        if (IsClientProfileLocked(profileName))
            throw new TrebException($"Profile {profileName} folder is currently locked by another process.");

        SetupJunction(setup.GetPrimaryJunction(), profile.ProfileFolder);

        await iniHandler.WriteClientSettingsAsync(profile);
        
        if (autoConnect)
        {
            if (!IsAutoConnectInfoValid(modlist))
                throw new Exception("Auto connection address is invalid");
            await iniHandler.WriteClientLastConnection(modlist.ServerAddress, modlist.ServerPort, modlist.ServerPassword);
        }
        
        var process = await CreateClientProcess(profile, modlist, isBattleEye, autoConnect);

        process.Start();

        var childProcess = await CatchClientChildProcess(process);
        if (childProcess == null)
            throw new TrebException("Could not launch the game");
        
        ConfigureProcess(profile.ProcessPriority, profile.CPUThreadAffinity, childProcess);

        return childProcess;
    }

    private bool IsAutoConnectInfoValid(ModListProfile profile)
    {
        if (!IPAddress.TryParse(profile.ServerAddress, out _)) return false;
        if (profile.ServerPort is < 0 or > 65535) return false;
        return true;
    }

    private async Task<Process> CreateClientProcess(ClientProfile profile, ModListProfile modlist, bool isBattleEye, bool autoConnect)
    {
        var filename = setup.GetBinFile(isBattleEye);
        var modlistFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(modlistFile, appFiles.Mods.GetResolvedModlist(modlist.Modlist));
        var args = profile.GetClientArgs(modlistFile, autoConnect);

        var dir = Path.GetDirectoryName(filename);
        if (dir == null)
            throw new Exception($"Failed to start process, invalid directory {filename}");

        var process = new Process();
        process.StartInfo.FileName = filename;
        process.StartInfo.WorkingDirectory = dir;
        process.StartInfo.Arguments = args;
        process.StartInfo.UseShellExecute = false;
        process.EnableRaisingEvents = true;

        return process;
    }

    private async Task<Process?> CatchClientChildProcess(Process parent)
    {
        var target = ProcessData.Empty;
        DateTime start = DateTime.UtcNow;
        while (target.IsEmpty && !parent.HasExited)
        {
            if ((DateTime.UtcNow - start).TotalSeconds > 20) return null;
            target = (await Tools.GetProcessesWithName(Constants.FileClientBin)).FirstOrDefault();
            await Task.Delay(25);
        }

        if (target.IsEmpty) return null;
        return !target.TryGetProcess(out var targetProcess) ? null : targetProcess;
    }

    private void ConfigureProcess(int priority, long threadAffinity, Process process)
    {
        process.PriorityClass = GetPriority(priority);
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            process.ProcessorAffinity = (IntPtr)Tools.Clamp2CPUThreads(threadAffinity);
    }

    private ProcessPriorityClass GetPriority(int index)
    {
        switch (index)
        {
            case 1:
                return ProcessPriorityClass.AboveNormal;

            case 2:
                return ProcessPriorityClass.High;

            case 3:
                return ProcessPriorityClass.RealTime;

            default:
                return ProcessPriorityClass.Normal;
        }
    }

    public async Task ReCatapultServer(int instance)
    {
        await CatapultServer(instance);
    }

    public async Task CatapultServer(int instance)
    {
        var profile = setup.Config.GetInstanceProfile(instance);
        var modlist = setup.Config.GetInstanceModlist(instance);
        await CatapultServer(profile, modlist, instance);
    }

    public async Task<Process> CatapultServerProcess(int instance)
    {
        var profile = setup.Config.GetInstanceProfile(instance);
        var modlist = setup.Config.GetInstanceModlist(instance);
        return await CatapultServerProcess(profile, modlist, instance);
    }
    
    /// <summary>
    ///     Launch a server process while taking care of everything. Generate the modlist, generate the ini settings, etc.
    ///     Process is created on a separate thread, and fire the event ServerProcessStarted when the process is running.
    /// </summary>
    /// <param name="profileName"></param>
    /// <param name="modlistName"></param>
    /// <param name="instance">Index of the instance you want to launch</param>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="ArgumentException">
    ///     Profiles can only be used by one process at a times, since they contain the db of
    ///     the game.
    /// </exception>
    public async Task CatapultServer(string profileName, string modlistName, int instance)
    {
        if (_serverProcesses.ContainsKey(instance)) return;

        var process = await CatapultServerProcess(profileName, modlistName, instance);
        var profile = appFiles.Server.Get(profileName);

        var builder = processFactory.Create()
            .SetProcess(process)
            .SetServerInfos(profile, instance)
            .SetLogFile(appFiles.Server.GetGameLogs(profileName))
            .StartLogAtBeginning();
        if (profile.EnableRCon)
            builder.UseRCon();
        
        _serverProcesses.TryAdd(instance, await builder.BuildServer());
    }
    
    public async Task<Process> CatapultServerProcess(string profileName, string modlistName, int instance)
    {
        var data = new Dictionary<string, object>
        {
            {@"profile", profileName},
            {"modlist", modlistName},
            {"instance", instance}
        };
        using var scope = logger.BeginScope(data);
        logger.LogInformation("Launching");
        
        if (!appFiles.Server.TryGet(profileName, out var profile))
            throw new FileNotFoundException($"{profileName} profile not found.");
        if (!appFiles.Mods.TryGet(modlistName, out var modlist))
            throw new FileNotFoundException($"{modlistName} modlist not found.");
        if (IsServerProfileLocked(profileName))
            throw new ArgumentException($"Profile {profileName} folder is currently locked by another process.");

        SetupJunction(Path.Combine(setup.GetInstancePath(instance), Constants.FolderGameSave), 
            profile.ProfileFolder);

        await iniHandler.WriteServerSettingsAsync(profile, instance);
        var process = await CreateServerProcess(instance, profile, modlist);
        process.Start();

        var childProcess = await CatchServerChildProcess(process);
        if (childProcess == null)
            throw new TrebException("Could not launch the server");

        ConfigureProcess(profile.ProcessPriority, profile.CPUThreadAffinity, childProcess);

        return childProcess;
    }

    private async Task<Process> CreateServerProcess(int instance, ServerProfile profile, ModListProfile modlist)
    {
        var process = new Process();

        var filename = setup.GetIntanceBinary(instance);
        
        var modfileFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(modfileFile, appFiles.Mods.GetResolvedModlist(modlist.Modlist));
        
        var args = profile.GetServerArgs(instance, modfileFile);

        var dir = Path.GetDirectoryName(filename);
        if (dir == null)
            throw new Exception($"Failed to start process, invalid directory {filename}");

        process.StartInfo.FileName = filename;
        process.StartInfo.WorkingDirectory = dir;
        process.StartInfo.Arguments = args;
        process.StartInfo.UseShellExecute = true;
        process.EnableRaisingEvents = true;
        return process;
    }

    private async Task<Process?> CatchServerChildProcess(Process process)
    {
        var child = ProcessData.Empty;
        DateTime start = DateTime.UtcNow;
        while (child.IsEmpty && !process.HasExited)
        {
            if ((DateTime.UtcNow - start).TotalSeconds > 20) return null;
            child = Tools.GetFirstChildProcesses(process.Id);
            await Task.Delay(25);
        }

        if (child.IsEmpty) return null;
        if (!child.TryGetProcess(out var targetProcess)) return null;
        return targetProcess;
    }

    public IEnumerable<int> GetActiveServers()
    {
        return _serverProcesses.Keys;
    }

    /// <summary>
    ///     Ask a particular server instance to close. If the process is borked, this will not work.
    /// </summary>
    /// <param name="instance"></param>
    public async Task CloseServer(int instance)
    {
        logger.LogInformation($"Close Server {instance}");
        if (_serverProcesses.TryGetValue(instance, out var watcher))
            await watcher.StopAsync();
    }

    public IConanProcess? GetClientProcess()
    {
        return _conanClientProcess;
    }

    public IRcon? GetServerRcon(int instance)
    {
        if (_serverProcesses.TryGetValue(instance, out var watcher))
            return watcher.RCon;
        throw new ArgumentException($"Server instance {instance} is not running.");
    }

    /// <summary>
    ///     Get the server port information for all the running server processes.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IConanServerProcess> GetServerProcesses()
    {
        foreach (var p in _serverProcesses.Values)
            yield return p;
    }

    public bool IsAnyServerRunning()
    {
        return _serverProcesses.Count > 0;
    }

    public bool IsClientRunning()
    {
        return _conanClientProcess != null;
    }

    /// <summary>
    ///     Kill the client process.
    /// </summary>
    public async Task KillClient()
    {
        if (_conanClientProcess == null) return;
        logger.LogInformation("Kill client");
        await _conanClientProcess.KillAsync();
    }

    /// <summary>
    ///     Kill a particular server instance.
    /// </summary>
    /// <param name="instance"></param>
    public async Task KillServer(int instance)
    {
        if (_serverProcesses.TryGetValue(instance, out var watcher))
        {
            logger.LogInformation($"Kill server {instance}");
            await watcher.KillAsync();
        }
    }

    public async Task Tick()
    {
        if (!_hasCatapulted && setup.Catapult)
        {
            _hasCatapulted = true;
            for (int i = 0; i < setup.Config.ServerInstanceCount; i++)
                await CatapultServer(i);
        }

        await CleanStoppedProcesses();
        await FindExistingClient();
        await FindExistingServers();

        if(_conanClientProcess is not null)
            await _conanClientProcess.RefreshAsync();
        foreach (var process in _serverProcesses.Values)
        {
            var name = setup.Config.GetInstanceProfile(process.Instance);
            if (appFiles.Server.Exists(name))
            {
                var profile = appFiles.Server.Get(name);
                process.KillZombies = profile.KillZombies;
                process.ZombieCheckSeconds = profile.ZombieCheckSeconds;
            }
            await process.RefreshAsync();
        }
    }

    public async Task<ConanClientProcessInfos?> FindClientProcess()
    {
        var data = (await Tools.GetProcessesWithName(Constants.FileClientBin)).FirstOrDefault();

        if (data.IsEmpty) return null;
        if (!data.TryGetProcess(out var process)) return null;

        return new ConanClientProcessInfos()
        {
            Process = process,
            Start = data.start
        };
    }
    
    public async IAsyncEnumerable<ConanServerProcessInfos> FindServerProcesses()
    {
        var processes = await Tools.GetProcessesWithName(Constants.FileServerBin);
        foreach (var p in processes)
        {
            if (!setup.TryGetInstanceIndexFromPath(p.filename, out var instance)) continue;
            if (!p.TryGetProcess(out var process)) continue;

            var gameLogs = Path.Combine(setup.GetInstancePath(instance),
                Constants.FolderGameSave,
                Constants.FolderGameSaveLog,
                Constants.FileGameLogFile);
            yield return new ConanServerProcessInfos()
            {
                Process = process,
                Start = p.start,
                Instance = instance,
                GameLogs = gameLogs
            };
        }
    }
    
    private async Task FindExistingClient()
    {
        if (_conanClientProcess != null) return;

        var process = await FindClientProcess();
        if (process is not null)
            _conanClientProcess = await processFactory.Create()
                .SetStartDate(process.Start)
                .SetProcess(process.Process)
                .BuildClient();
    }

    private async Task FindExistingServers()
    {
        await foreach (var process in FindServerProcesses())
        {
            if(_serverProcesses.ContainsKey(process.Instance)) continue;
            var serverInfos = await iniHandler.GetInfosFromServerAsync(process.Instance);
            var builder = processFactory.Create()
                .SetStartDate(process.Start)
                .SetProcess(process.Process)
                .SetServerInfos(serverInfos)
                .SetLogFile(process.GameLogs);
            if (serverInfos.RConPort > 0)
                builder.UseRCon();
            _serverProcesses.TryAdd(process.Instance, await builder.BuildServer());
        }
    }

    private async Task CleanStoppedProcesses()
    {
        if (_conanClientProcess != null && !_conanClientProcess.State.IsRunning())
        {
            logger.LogInformation("Client stopped");
            _conanClientProcess.Dispose();
            _conanClientProcess = null;
        }

        foreach (var server in _serverProcesses.ToList())
        {
            if (!server.Value.State.IsRunning())
            {
                logger.LogInformation("Server {instance} stopped", server.Key);
                _serverProcesses.Remove(server.Key);
                var name = setup.Config.GetInstanceProfile(server.Key);
                if (server.Value.State == ProcessState.CRASHED && appFiles.Server.Get(name).RestartWhenDown)
                {
                    await ReCatapultServer(server.Key);
                }
                server.Value.Dispose();
            }
        }
    }

    public bool IsClientProfileLocked(string profileName)
    {
        if (_conanClientProcess == null) return false;
        var junction = Path.GetFullPath(GetCurrentClientJunction());
        var profilePath = Path.GetFullPath(appFiles.Client.GetDirectory(profileName));
        return string.Equals(junction, profilePath, StringComparison.Ordinal);
    }

    public bool IsServerProfileLocked(string profileName)
    {
        var profilePath = Path.GetFullPath(appFiles.Server.GetDirectory(profileName));
        foreach (var s in _serverProcesses.Values)
        {
            var instance = s.Instance;
            var junction = Path.GetFullPath(GetCurrentServerJunction(instance));
            if (string.Equals(junction, profilePath, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private string GetCurrentClientJunction()
    {
        var path = Path.Combine(setup.GetClientFolder(), Constants.FolderGameSave);
        if (JunctionPoint.Exists(path))
            return JunctionPoint.GetTarget(path);
        return string.Empty;
    }

    private string GetCurrentServerJunction(int instance)
    {
        var path = Path.Combine(setup.GetInstancePath(instance), Constants.FolderGameSave);
        if (JunctionPoint.Exists(path))
            return JunctionPoint.GetTarget(path);
        return string.Empty;
    }

    private void SetupJunction(string junction, string targetPath)
    {
        logger.LogInformation("Setup new junction {junction} > {target}", junction, targetPath);
        Tools.RemoveSymboliclink(junction);
        Tools.SetupSymboliclink(junction, targetPath);
    }
}