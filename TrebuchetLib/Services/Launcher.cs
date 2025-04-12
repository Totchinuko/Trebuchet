using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using tot_lib;
using TrebuchetLib.Processes;

namespace TrebuchetLib.Services;

public class Launcher(AppFiles appFiles, AppSetup setup, IIniGenerator iniHandler, ILogger<Launcher> logger)
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

    public async Task CatapultClient(bool isBattleEye)
    {
        var profile = setup.Config.SelectedClientProfile;
        var modlist = setup.Config.SelectedClientModlist;
        await CatapultClient(profile, modlist, isBattleEye);
    }

    public async Task CatapultClientBoulder(bool isBattleEye)
    {
        var profile = setup.Config.SelectedClientProfile;
        var modlist = setup.Config.SelectedClientModlist;
        await CatapultClientBoulder(profile, modlist, isBattleEye);
    }

    public async Task<Process> CatapultClientProcess(bool isBattleEye)
    {
        var profile = setup.Config.SelectedClientProfile;
        var modlist = setup.Config.SelectedClientModlist;
        return await CatapultClientProcess(profile, modlist, isBattleEye);
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
    public async Task CatapultClient(string profileName, string modlistName, bool isBattleEye)
    {
        if (_conanClientProcess != null) return;

        var process = await CatapultClientProcess(profileName, modlistName, isBattleEye);

        _conanClientProcess = new ConanClientProcess(process);
    }
    
    public async Task CatapultClientBoulder(string profile, string modlist, bool battleEye)
    {
        if (_conanClientProcess != null) return;
        
        string? appFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        if (appFolder is null) throw new IOException("Can't access app folder");
        var boulder = Path.Combine(appFolder, Constants.BoulderExe);
        if (!File.Exists(boulder)) throw new IOException("boulder not found");

        var args = new List<string>
        {
            Constants.cmdBoulderLambClient,
            $"{Constants.argBoulderSave} \"{profile}\"",
            $"{Constants.argBoulderModlist} \"{modlist}\""
        };
        if(battleEye)
            args.Add(Constants.argBoulderBattleEye);
        
        Process startProcess = new();
        startProcess.StartInfo = new ProcessStartInfo
        {
            Arguments = string.Join(' ', args),
            FileName = boulder,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        startProcess.Start();
        await startProcess.WaitForExitAsync();
    }

    public async Task<Process> CatapultClientProcess(string profileName, string modlistName, bool isBattleEye)
    {

        if (!appFiles.Client.TryGet(profileName, out var profile))
            throw new TrebException($"{profileName} profile not found.");
        if (!appFiles.Mods.TryGet(modlistName, out var modlist))
            throw new TrebException($"{modlistName} modlist not found.");
        if (IsClientProfileLocked(profileName))
            throw new TrebException($"Profile {profileName} folder is currently locked by another process.");

        SetupJunction(appFiles.Client.GetPrimaryJunction(), profile.ProfileFolder);

        logger.LogDebug($"Locking folder {profile.ProfileName}");
        logger.LogInformation($"Launching client process with profile {profileName} and modlist {modlistName}");

        await iniHandler.WriteClientSettingsAsync(profile);
        var process = await CreateClientProcess(profile, modlist, isBattleEye);

        await Task.Run(() => process.Start());

        var childProcess = await CatchClientChildProcess(process);
        if (childProcess == null)
            throw new TrebException("Could not launch the game");
        
        ConfigureProcess(profile.ProcessPriority, profile.CPUThreadAffinity, childProcess);

        return childProcess;
    }

    private async Task<Process> CreateClientProcess(ClientProfile profile, ModListProfile modlist, bool isBattleEye)
    {
        var filename = isBattleEye ? appFiles.Client.GetBattleEyeBinaryPath() : appFiles.Client.GetGameBinaryPath();
        var modlistFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(modlistFile, appFiles.Mods.GetResolvedModlist(modlist.Modlist));
        var args = profile.GetClientArgs(modlistFile);

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
    
    public async Task CatapultServerBoulder(int instance)
    {
        var profile = setup.Config.GetInstanceProfile(instance);
        var modlist = setup.Config.GetInstanceModlist(instance);
        await CatapultServerBoulder(profile, modlist, instance);
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

        var serverInfos = new ConanServerInfos(appFiles.Server.Get(profileName), instance);
        var conanServerProcess = new ConanServerProcess(process, serverInfos);
        _serverProcesses.TryAdd(instance, conanServerProcess);
    }
    
    public async Task CatapultServerBoulder(string profile, string modlist, int instance)
    {
        if (_serverProcesses.ContainsKey(instance)) return;
        
        string? appFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        if (appFolder is null) throw new IOException("Can't access app folder");
        var boulder = Path.Combine(appFolder, Constants.BoulderExe);
        if (!File.Exists(boulder)) throw new IOException("boulder not found");

        var args = new List<string>
        {
            Constants.cmdBoulderLambServer,
            $"{Constants.argBoulderSave} \"{profile}\"",
            $"{Constants.argBoulderInstance} {instance}",
            $"{Constants.argBoulderModlist} \"{modlist}\""
        };

        Process startProcess = new();
        startProcess.StartInfo = new()
        {
            Arguments = string.Join(' ', args),
            FileName = boulder,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        startProcess.Start();
        await startProcess.WaitForExitAsync();
    }

    public async Task<Process> CatapultServerProcess(string profileName, string modlistName, int instance)
    {
        if (!appFiles.Server.TryGet(profileName, out var profile))
            throw new FileNotFoundException($"{profileName} profile not found.");
        if (!appFiles.Mods.TryGet(modlistName, out var modlist))
            throw new FileNotFoundException($"{modlistName} modlist not found.");
        if (IsServerProfileLocked(profileName))
            throw new ArgumentException($"Profile {profileName} folder is currently locked by another process.");

        SetupJunction(Path.Combine(appFiles.Server.GetInstancePath(instance), Constants.FolderGameSave), 
            profile.ProfileFolder);

        logger.LogDebug($"Locking folder {profile.ProfileName}");
        logger.LogInformation(
            $"Launching server process with profile {profileName} and modlist {modlistName} on instance {instance}");

        await iniHandler.WriteServerSettingsAsync(profile, instance);
        var process = await CreateServerProcess(instance, profile, modlist);

        await Task.Run(() => process.Start());

        var childProcess = await CatchServerChildProcess(process);
        if (childProcess == null)
            throw new TrebException("Could not launch the server");

        ConfigureProcess(profile.ProcessPriority, profile.CPUThreadAffinity, childProcess);

        return childProcess;
    }



    private async Task<Process> CreateServerProcess(int instance, ServerProfile profile, ModListProfile modlist)
    {
        var process = new Process();

        var filename = appFiles.Server.GetIntanceBinary(instance);
        
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
        logger.LogInformation($"Requesting server instance {instance} stop");
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
        logger.LogInformation("Requesting client process kill");
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
            logger.LogInformation($"Requesting server process kill on instance {instance}");
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
            if (!appFiles.Server.TryGetInstanceIndexFromPath(p.filename, out var instance)) continue;
            if (_serverProcesses.ContainsKey(instance)) continue;
            if (!p.TryGetProcess(out var process)) continue;

            var infos = await iniHandler.GetInfosFromServerAsync(instance).ConfigureAwait(false);
            IConanServerProcess server = new ConanServerProcess(process, infos, p.start);
            _serverProcesses.TryAdd(instance, server);
        }
    }

    private async Task CleanStoppedProcesses()
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
        var profilePath = Path.GetFullPath(appFiles.Client.GetFolder(profileName));
        return string.Equals(junction, profilePath, StringComparison.Ordinal);
    }

    public bool IsServerProfileLocked(string profileName)
    {
        var profilePath = Path.GetFullPath(appFiles.Server.GetFolder(profileName));
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
        var path = Path.Combine(appFiles.Client.GetClientFolder(), Constants.FolderGameSave);
        if (JunctionPoint.Exists(path))
            return JunctionPoint.GetTarget(path);
        return string.Empty;
    }

    private string GetCurrentServerJunction(int instance)
    {
        var path = Path.Combine(appFiles.Server.GetInstancePath(instance), Constants.FolderGameSave);
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