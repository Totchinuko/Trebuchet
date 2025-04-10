using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TrebuchetLib.Processes;
using TrebuchetUtils;

namespace TrebuchetLib.Services;

public class Launcher : IDisposable
{
    private readonly AppFiles _appFiles;
    private readonly AppSetup _setup;
    private readonly IIniGenerator _iniHandler;
    private readonly ILogger<Launcher> _logger;
    private readonly Dictionary<int, IConanServerProcess> _serverProcesses = [];
    private IConanProcess? _conanClientProcess;
    private bool _hasCatapulted;

    public Launcher(AppFiles appFiles, AppSetup setup, IIniGenerator iniHandler, ILogger<Launcher> logger)
    {
        _appFiles = appFiles;
        _setup = setup;
        _iniHandler = iniHandler;
        _logger = logger;
    }

    public void Dispose()
    {
        _conanClientProcess?.Dispose();
        foreach (var item in _serverProcesses)
            item.Value.Dispose();
        _serverProcesses.Clear();
    }

    public async Task CatapultClient(bool isBattleEye)
    {
        var profile = _setup.Config.SelectedClientProfile;
        var modlist = _setup.Config.SelectedClientModlist;
        await CatapultClient(profile, modlist, isBattleEye);
    }

    /// <summary>
    ///     Launch a client process while taking care of everything. Generate the modlist, generate the ini settings, etc.
    ///     Process is created on a separate thread, and fire the event ClientProcessStarted when the process is running.
    /// </summary>
    /// <param name="profileName"></param>
    /// <param name="modlistName"></param>
    /// <param name="isBattleEye">Launch with BattlEye anti cheat.</param>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="ArgumentException">
    ///     Profiles can only be used by one process at a times, since they contain the db of
    ///     the game.
    /// </exception>
    internal async Task CatapultClient(string profileName, string modlistName, bool isBattleEye)
    {
        if (_conanClientProcess != null) return;

        if (!_appFiles.Client.TryGet(profileName, out var profile))
            throw new TrebException($"{profileName} profile not found.");
        if (!_appFiles.Mods.TryGet(modlistName, out var modlist))
            throw new TrebException($"{modlistName} modlist not found.");
        if (IsClientProfileLocked(profileName))
            throw new TrebException($"Profile {profileName} folder is currently locked by another process.");

        SetupJunction(_appFiles.Client.GetPrimaryJunction(), profile.ProfileFolder);

        _logger.LogDebug($"Locking folder {profile.ProfileName}");
        _logger.LogInformation($"Launching client process with profile {profileName} and modlist {modlistName}");

        var tmpFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmpFile, _appFiles.Mods.GetResolvedModlist(modlist.Modlist));
        await _iniHandler.WriteClientSettingsAsync(profile);
        var process = CreateClientProcess(profile, tmpFile, isBattleEye);

        process.Start();

        var childProcess = await CatchClientChildProcess(process);
        if (childProcess == null)
            throw new TrebException("Could not launch the game");

        ConfigureProcess(profile.ProcessPriority, profile.CPUThreadAffinity, childProcess);

        _conanClientProcess = new ConanClientProcess(childProcess);
    }

    private Process CreateClientProcess(ClientProfile profile, string modlistPath, bool isBattleEye)
    {
        var filename = isBattleEye ? _appFiles.Client.GetBattleEyeBinaryPath() : _appFiles.Client.GetGameBinaryPath();
        var args = profile.GetClientArgs(modlistPath);

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
        while (target.IsEmpty && !parent.HasExited)
        {
            target = (await Tools.GetProcessesWithName(Constants.FileClientBin)).FirstOrDefault();
            await Task.Delay(50);
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

    public async Task CatapultServer(int instance)
    {
        var profile = _setup.Config.GetInstanceProfile(instance);
        var modlist = _setup.Config.GetInstanceModlist(instance);
        await CatapultServer(profile, modlist, instance);
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
    internal async Task CatapultServer(string profileName, string modlistName, int instance)
    {
        if (_serverProcesses.ContainsKey(instance)) return;

        if (!_appFiles.Server.TryGet(profileName, out var profile))
            throw new FileNotFoundException($"{profileName} profile not found.");
        if (!_appFiles.Mods.TryGet(modlistName, out var modlist))
            throw new FileNotFoundException($"{modlistName} modlist not found.");
        if (IsServerProfileLocked(profileName))
            throw new ArgumentException($"Profile {profileName} folder is currently locked by another process.");

        SetupJunction(_appFiles.Server.GetInstancePath(instance), profile.ProfileFolder);

        _logger.LogDebug($"Locking folder {profile.ProfileName}");
        _logger.LogInformation(
            $"Launching server process with profile {profileName} and modlist {modlistName} on instance {instance}");

        var tmpFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmpFile, _appFiles.Mods.GetResolvedModlist(modlist.Modlist));
        await _iniHandler.WriteServerSettingsAsync(profile, instance);
        var process = CreateServerProcess(instance, tmpFile, profile);
        process.Start();

        var childProcess = await CatchServerChildProcess(process);
        if (childProcess == null)
            throw new TrebException("Could not launch the server");

        ConfigureProcess(profile.ProcessPriority, profile.CPUThreadAffinity, childProcess);

        var serverInfos = new ConanServerInfos(profile, instance);
        var conanServerProcess = new ConanServerProcess(childProcess, serverInfos);
        _serverProcesses.TryAdd(instance, conanServerProcess);
    }

    private Process CreateServerProcess(int instance, string modlistPath, ServerProfile profile)
    {
        var process = new Process();

        var filename = _appFiles.Server.GetIntanceBinary(instance);
        var args = profile.GetServerArgs(instance, modlistPath);

        var dir = Path.GetDirectoryName(filename);
        if (dir == null)
            throw new Exception($"Failed to start process, invalid directory {filename}");

        process.StartInfo.FileName = filename;
        process.StartInfo.WorkingDirectory = dir;
        process.StartInfo.Arguments = args;
        process.StartInfo.UseShellExecute = false;
        process.EnableRaisingEvents = true;
        return process;
    }

    private async Task<Process?> CatchServerChildProcess(Process process)
    {
        var child = ProcessData.Empty;
        while (child.IsEmpty && !process.HasExited)
        {
            child = Tools.GetFirstChildProcesses(process.Id);
            await Task.Delay(50);
        }

        if (child.IsEmpty) return null;
        if (!child.TryGetProcess(out var targetProcess)) return null;
        return targetProcess;
    }

    /// <summary>
    ///     Ask a particular server instance to close. If the process is borked, this will not work.
    /// </summary>
    /// <param name="instance"></param>
    public async Task CloseServer(int instance)
    {
        _logger.LogInformation($"Requesting server instance {instance} stop");
        if (_serverProcesses.TryGetValue(instance, out var watcher))
            await watcher.StopAsync();
    }

    public IConanProcess? GetClientProcess()
    {
        return _conanClientProcess;
    }

    public IConsole GetServerConsole(int instance)
    {
        if (_serverProcesses.TryGetValue(instance, out var watcher))
            return watcher.Console;
        throw new ArgumentException($"Server instance {instance} is not running.");
    }

    public IRcon GetServerRcon(int instance)
    {
        if (_serverProcesses.TryGetValue(instance, out var watcher))
            return watcher.RCon;
        throw new ArgumentException($"Server instance {instance} is not running.");
    }

    /// <summary>
    ///     Get the server port informations for all the running server processes.
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
        _logger.LogInformation("Requesting client process kill");
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
            _logger.LogInformation($"Requesting server process kill on instance {instance}");
            await watcher.KillAsync();
        }
    }

    public async Task Tick()
    {
        if (!_hasCatapulted && _setup.Catapult)
        {
            _hasCatapulted = true;
            for (int i = 0; i < _setup.Config.ServerInstanceCount; i++)
                await CatapultServer(i);
        }

        CleanStoppedProcesses();
        await FindExistingClient();
        await FindExistingServers();

        if(_conanClientProcess is not null)
            await _conanClientProcess.RefreshAsync();
        foreach (var process in _serverProcesses.Values)
        {
            var name = _setup.Config.GetInstanceProfile(process.Instance);
            if (_appFiles.Server.Exists(name))
            {
                var profile = _appFiles.Server.Get(name);
                process.KillZombies = profile.KillZombies;
                process.ZombieCheckSeconds = profile.ZombieCheckSeconds;
            }
            await process.RefreshAsync();
        }
    }

    private async Task FindExistingClient()
    {
        var data = (await Tools.GetProcessesWithName(Constants.FileClientBin)).FirstOrDefault();

        if (_conanClientProcess != null) return;
        if (data.IsEmpty) return;
        if (!data.TryGetProcess(out var process)) return;

        IConanProcess client = new ConanClientProcess(process, data.start);
        _conanClientProcess = client;
    }

    private async Task FindExistingServers()
    {
        var processes = await Tools.GetProcessesWithName(Constants.FileServerBin);
        foreach (var p in processes)
        {
            if (!_appFiles.Server.TryGetInstanceIndexFromPath(p.filename, out var instance)) continue;
            if (_serverProcesses.ContainsKey(instance)) continue;
            if (!p.TryGetProcess(out var process)) continue;

            var infos = await _iniHandler.GetInfosFromServerAsync(instance).ConfigureAwait(false);
            IConanServerProcess server = new ConanServerProcess(process, infos, p.start);
            _serverProcesses.TryAdd(instance, server);
        }
    }

    private void CleanStoppedProcesses()
    {
        if (_conanClientProcess != null && !_conanClientProcess.State.IsRunning())
        {
            _conanClientProcess.Dispose();
            _conanClientProcess = null;
        }

        foreach (var server in _serverProcesses.ToList())
        {
            if (!server.Value.State.IsRunning())
            {
                server.Value.Dispose();
                _serverProcesses.Remove(server.Key);
            }
        }
    }

    public bool IsClientProfileLocked(string profileName)
    {
        if (_conanClientProcess == null) return false;
        var junction = Path.GetFullPath(GetCurrentClientJunction());
        var profilePath = Path.GetFullPath(_appFiles.Client.GetFolder(profileName));
        return string.Equals(junction, profilePath, StringComparison.Ordinal);
    }

    public bool IsServerProfileLocked(string profileName)
    {
        var profilePath = Path.GetFullPath(_appFiles.Server.GetFolder(profileName));
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
        var path = Path.Combine(_appFiles.Client.GetClientFolder(), Constants.FolderGameSave);
        if (JunctionPoint.Exists(path))
            return JunctionPoint.GetTarget(path);
        return string.Empty;
    }

    private string GetCurrentServerJunction(int instance)
    {
        var path = Path.Combine(_appFiles.Server.GetInstancePath(instance), Constants.FolderGameSave);
        if (JunctionPoint.Exists(path))
            return JunctionPoint.GetTarget(path);
        return string.Empty;
    }

    private void SetupJunction(string junction, string targetPath)
    {
        Tools.RemoveSymboliclink(junction);
        Tools.SetupSymboliclink(junction, targetPath);
    }
}