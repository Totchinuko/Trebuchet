﻿using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using tot_lib;
using TrebuchetLib.Processes;

namespace TrebuchetLib.Services;

public class Launcher : IDisposable
{
    public Launcher(AppFiles appFiles, 
        AppSetup setup, 
        Steam steam,
        IIniGenerator iniHandler, 
        ConanProcessFactory processFactory,
        ILogger<Launcher> logger)
    {
        _appFiles = appFiles;
        _setup = setup;
        _steam = steam;
        _iniHandler = iniHandler;
        _processFactory = processFactory;
        _logger = logger;

        _startDates = LoadStartDates();
    }
    
    private readonly Dictionary<int, IConanServerProcess> _serverProcesses = [];
    private IConanProcess? _conanClientProcess;
    private bool _hasCatapulted;
    private readonly List<IPRefWithModList> _modListNeedUpdate = [];
    private bool _serverNeedUpdate;
    private DateTime _lastUpdateCheckTime;
    private List<StartDates> _startDates;
    private readonly AppFiles _appFiles;
    private readonly AppSetup _setup;
    private readonly Steam _steam;
    private readonly IIniGenerator _iniHandler;
    private readonly ConanProcessFactory _processFactory;
    private readonly ILogger<Launcher> _logger;

    public event EventHandler? StateChanged;

    public void Dispose()
    {
        _conanClientProcess?.Dispose();
        foreach (var item in _serverProcesses)
            item.Value.Dispose();
        _serverProcesses.Clear();
    }

    public async Task CatapultClient(bool isBattleEye, ClientConnectionRef? autoConnect)
    {
        var profile = _appFiles.Client.Resolve(_setup.Config.SelectedClientProfile);
        var modlist = _appFiles.ResolveModList(_setup.Config.SelectedClientModlist);
        await CatapultClient(profile, modlist, isBattleEye, autoConnect);
    }

    public async Task<Process> CatapultClientProcess(bool isBattleEye, ClientConnectionRef? autoConnect)
    {
        var profile = _appFiles.Client.Resolve(_setup.Config.SelectedClientProfile);
        var modlist = _appFiles.ResolveModList(_setup.Config.SelectedClientModlist);
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
    public async Task CatapultClient(ClientProfileRef profileName, IPRefWithModList modlistName, bool isBattleEye, ClientConnectionRef? autoConnect)
    {
        if (_conanClientProcess != null) return;

        var process = await CatapultClientProcess(profileName, modlistName, isBattleEye, autoConnect);

        _conanClientProcess = await _processFactory.Create().SetProcess(process).BuildClient();
        OnStateChanged();
    }

    public async Task<Process> CatapultClientProcess(ClientProfileRef profileRef, IPRefWithModList modListRef, bool isBattleEye, ClientConnectionRef? autoConnect)
    {
        var data = new Dictionary<string, object>
        {
            {@"profile", profileRef},
            {"modlist", modListRef},
            {"isBattleEye", isBattleEye}
        };
        if (autoConnect is not null)
            data["autoConnect"] = autoConnect.Connection;
        
        using var scope = _logger.BeginScope(data);
        _logger.LogInformation("Launching");
        
        if (!_appFiles.Client.TryGet(profileRef, out var profile))
            throw new Exception($"{profileRef} profile not found.");
        if (!modListRef.TryGetModList(out var modList))
            throw new Exception($"{modListRef} modlist not found.");
        if (IsClientProfileLocked(profileRef))
            throw new Exception($"Profile {profileRef} folder is currently locked by another process.");

        SetupJunction(_setup.GetPrimaryJunction(), profile.ProfileFolder);

        await _iniHandler.WriteClientSettingsAsync(profile);
        
        if (autoConnect is not null && autoConnect.TryGet(out var connection))
        {
            if (!IsAutoConnectInfoValid(connection))
                throw new Exception("Auto connection address is invalid");
            await _iniHandler.WriteClientLastConnection(connection);
        }
        
        var process = await CreateClientProcess(profile, modList, isBattleEye, autoConnect is not null);

        process.Start();

        var childProcess = await CatchClientChildProcess(process);
        if (childProcess == null)
            throw new Exception("Could not launch the game");
        
        ConfigureProcess(profile.ProcessPriority, profile.CPUThreadAffinity, childProcess);

        return childProcess;
    }

    public async Task CatapultServer(int instance)
    {
        var profile = _appFiles.Server.Resolve(_setup.Config.GetInstanceProfile(instance));
        var modlist = _appFiles.ResolveModList(_setup.Config.GetInstanceModlist(instance));
        await CatapultServer(profile, modlist, instance);
    }

    public async Task<Process> CatapultServerProcess(int instance)
    {
        var profile = _appFiles.Server.Resolve(_setup.Config.GetInstanceProfile(instance));
        var modlist = _appFiles.ResolveModList(_setup.Config.GetInstanceModlist(instance));
        return await CatapultServerProcess(profile, modlist, instance);
    }
    
    /// <summary>
    ///     Launch a server process while taking care of everything. Generate the modlist, generate the ini settings, etc.
    ///     Process is created on a separate thread, and fire the event ServerProcessStarted when the process is running.
    /// </summary>
    /// <param name="profileName"></param>
    /// <param name="listRef"></param>
    /// <param name="instance">Index of the instance you want to launch</param>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="ArgumentException">
    ///     Profiles can only be used by one process at a times, since they contain the db of
    ///     the game.
    /// </exception>
    public async Task CatapultServer(ServerProfileRef profileName, IPRefWithModList listRef, int instance)
    {
        if (_serverProcesses.ContainsKey(instance)) return;

        var process = await CatapultServerProcess(profileName, listRef, instance);
        var profile = _appFiles.Server.Get(profileName);

        var builder = _processFactory.Create()
            .SetProcess(process)
            .SetServerInfos(profile, instance)
            .SetLogFile(_appFiles.Server.GetGameLogs(profileName))
            .StartLogAtBeginning();
        if (profile.EnableRCon)
            builder.UseRCon();
        
        _serverProcesses.TryAdd(instance, await builder.BuildServer());
        OnStateChanged();
    }
    
    public async Task<Process> CatapultServerProcess(ServerProfileRef profileName, IPRefWithModList listRef, int instance)
    {
        var data = new Dictionary<string, object>
        {
            {@"profile", profileName},
            {"modlist", listRef.Uri.OriginalString},
            {"instance", instance}
        };
        using var scope = _logger.BeginScope(data);
        _logger.LogInformation("Launching");
        
        if (!_appFiles.Server.TryGet(profileName, out var profile))
            throw new FileNotFoundException($"{profileName} profile not found.");
        if (!listRef.TryGetModList(out var list))
            throw new FileNotFoundException($"{listRef} modlist not found.");
        if (IsServerProfileLocked(profileName))
            throw new ArgumentException($"Profile {profileName} folder is currently locked by another process.");

        if (!await PerformCatapultUpdates())
            throw new Exception("Pre-launch update failed");
        
        SetupJunction(Path.Combine(_setup.GetInstancePath(instance), Constants.FolderGameSave), 
            profile.ProfileFolder);

        await _iniHandler.WriteServerSettingsAsync(profile, instance);
        var process = await CreateServerProcess(instance, profile, list);
        process.Start();

        var childProcess = await CatchServerChildProcess(process);
        if (childProcess == null)
            throw new Exception("Could not launch the server");

        ConfigureProcess(profile.ProcessPriority, profile.CPUThreadAffinity, childProcess);

        AddStartDate(instance);
        return childProcess;
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
        _logger.LogInformation($"Close Server {instance}");
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
        _logger.LogInformation("Kill client");
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
            _logger.LogInformation($"Kill server {instance}");
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

        await CleanStoppedProcesses();
        await FindExistingClient();
        await FindExistingServers();
        await PerformPeriodicUpdateCheck();
        await PerformAutomaticRestarts();

        if(_conanClientProcess is not null)
            await _conanClientProcess.RefreshAsync();
        foreach (var process in _serverProcesses.Values)
        {
            var name = _appFiles.Server.Resolve(_setup.Config.GetInstanceProfile(process.Instance));
            if (_appFiles.Server.Exists(name))
            {
                var profile = _appFiles.Server.Get(name);
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
            if (!_setup.TryGetInstanceIndexFromPath(p.filename, out var instance)) continue;
            if (!p.TryGetProcess(out var process)) continue;

            var gameLogs = Path.Combine(_setup.GetInstancePath(instance),
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

    public bool HasModListUpdates(IPRefWithModList modList)
    {
        return _modListNeedUpdate.Contains(modList);
    }

    public bool HasServerUpdate()
    {
        return _serverNeedUpdate;
    }

    public async Task<bool> PerformCatapultUpdates()
    {
        if (_setup.Config.AutoUpdateStatus == AutoUpdateStatus.Never) return true;
        if (IsAnyServerRunning() || IsClientRunning()) return true;
        if (!await UpdateMods()) return false;
        if (!await UpdateServers()) return false;
        return true;
    }

    public async Task<bool> VerifyFiles()
    {
        if (IsAnyServerRunning() || IsClientRunning()) return false;
        _logger.LogInformation(@"Verifying files, clearing caches");
        _steam.ClearCache();
        _steam.InvalidateCache();
        
        try
        {
            await UpdateServers();
            await UpdateMods();
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify files");
            return false;
        }
    }

    public Task<bool> UpdateMods()
    {
        _modListNeedUpdate.Clear();
        var lists = new List<IPRefWithModList>();
        for (int i = 0; i < _setup.Config.ServerInstanceCount; i++)
        {
            if(_appFiles.TryParseModListRef(_setup.Config.GetInstanceModlist(i), out var modListRef))
                lists.Add(modListRef);
        }
        if(_appFiles.TryParseModListRef(_setup.Config.SelectedClientModlist, out var clientModList))
            lists.Add(clientModList);
        if (lists.Count == 0) return Task.FromResult(true);
        return UpdateMods(lists.GetModsFromList().ToList());
    }
    
    public async Task<bool> UpdateMods(List<ulong> mods)
    {
        if (IsAnyServerRunning() || IsClientRunning()) return false;
        
        try
        {
            using(_logger.BeginScope(("mods", mods)))
                _logger.LogInformation("Updating mods");
            await _steam.UpdateMods(mods);
            await CheckModUpdates();
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update mods");
            return false;
        }
    }

    public async Task<bool> UpdateServers()
    {
        if (IsAnyServerRunning() || IsClientRunning()) return false;

        try
        {
            _logger.LogInformation("Updating servers");
            await _steam.UpdateServerInstances();
            await CheckServerUpdate();
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update servers");
            return false;
        }
    }
    
    public async Task<bool> CheckModUpdates()
    {
        try
        {
            _logger.LogInformation("Checking mod updates");
            _modListNeedUpdate.Clear();
            var lists = new List<IPRefWithModList>();
            for (int i = 0; i < _setup.Config.ServerInstanceCount; i++)
            {
                if(_appFiles.TryParseModListRef(_setup.Config.GetInstanceModlist(i), out var modListRef))
                    lists.Add(modListRef);
            }
            if(_appFiles.TryParseModListRef(_setup.Config.SelectedClientModlist, out var clientModList))
                lists.Add(clientModList);
            if (lists.Count == 0) return true;
            
            var details = await _steam.RequestModDetails(lists.GetModsFromList().ToList());
            if (details.Count == 0) return true;
            
            var updates = details
                .Where(d => d.Status.Status != UGCStatus.UpToDate)
                .Select(x => x.PublishedFileId)
                .ToList();
            _modListNeedUpdate.AddRange(lists.Where(x => x.GetModsFromList().Intersect(updates).Any()));
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check mod updates");
            return false;
        }
    }

    private async Task PerformAutomaticRestarts()
    {
        foreach (var instance in _serverProcesses.Values)
        {
            if(!instance.State.IsRunning() || instance.State.IsStopping()) continue;
            
            var profileUri = _setup.Config.GetInstanceProfile(instance.Instance);
            var serverPRef = _appFiles.Server.Resolve(profileUri);
            var serverProfile = serverPRef.Get();
            
            if(!serverProfile.AutoRestart) continue;
            
            var minUptime = serverProfile.AutoRestartMinUptime.TotalMinutes < 10
                ? TimeSpan.FromMinutes(10)
                : serverProfile.AutoRestartMinUptime;
            if((DateTime.UtcNow - instance.StartUtc ) < minUptime) continue;
            
            if(StartDateCountToday(instance.Instance) >= serverProfile.AutoRestartMaxPerDay
               && serverProfile.AutoRestartMaxPerDay > 0) continue;

            var time = DateTime.Now.TimeOfDay;
            if(serverProfile.AutoRestartDailyTime
               .All(x => time < x || time > x + TimeSpan.FromMinutes(5))) continue;

            await instance.RestartAsync();
        } 
    }

    private List<StartDates> LoadStartDates()
    {
        try
        {
            var path = Path.Combine(_setup.GetBaseInstancePath(),
                Constants.FileStartDateJson
            );
            if (!File.Exists(path)) return [];

            var json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json)) return [];

            var result = JsonSerializer.Deserialize<List<StartDates>>(json);
            if (result is null) return [];
            return result;
        }
        catch
        {
            return [];
        }
    }

    private void AddStartDate(int instance)
    {
        _startDates.Add(new StartDates(instance, DateTime.UtcNow));
        while(_startDates.Count > 50) 
            _startDates.RemoveAt(0);

        try
        {
            var json = JsonSerializer.Serialize(_startDates);
            var path = Path.Combine(_setup.GetBaseInstancePath(),
                Constants.FileStartDateJson
            );
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save start dates");
        }
    }

    private int StartDateCount(TimeSpan timeSpan, int instance)
    {
        var date = DateTime.UtcNow - timeSpan;
        return _startDates.Count(x => x.Date > date && x.Instance == instance);
    }

    private int StartDateCountToday(int instance)
    {
        return StartDateCount(DateTime.Now.TimeOfDay, instance);
    }

    private async Task PerformPeriodicUpdateCheck()
    {
        if ((DateTime.UtcNow - _lastUpdateCheckTime) >= _setup.Config.UpdateCheckFrequency)
        {
            if (!await CheckModUpdates()) return;
            if (!await CheckServerUpdate()) return;
            _lastUpdateCheckTime = DateTime.UtcNow;
        }

        if (_setup.Config.AutoUpdateStatus != AutoUpdateStatus.CheckForUpdates) return;
        if (IsClientRunning()) return; // can't auto-update if any client is running
        
        foreach (var process in _serverProcesses.Values)
        {
            if(!process.State.IsRunning() || process.State.IsStopping()) continue;
            
            if (_serverNeedUpdate)
            {
                await process.RestartAsync();
                continue;
            }
                    
            var modListRef = _appFiles.ResolveModList(_setup.Config.GetInstanceModlist(process.Instance));
            if (_modListNeedUpdate.Contains(modListRef))
                await process.RestartAsync();
        }
    }

    private async Task<bool> CheckServerUpdate()
    {
        _logger.LogInformation("Checking server updates");
        if (_steam.GetInstalledInstances() < _setup.Config.ServerInstanceCount)
        {
            _serverNeedUpdate = true;
            return true;
        }
        
        try
        {
            _serverNeedUpdate = await _steam.GetSteamBuildId() != _steam.GetInstanceBuildId(0);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check server updates");
            return false;
        }
    }
    
    private bool IsAutoConnectInfoValid(ClientConnection connection)
    {
        if (!IPAddress.TryParse(connection.IpAddress, out _)) return false;
        if (connection.Port is < 0 or > 65535) return false;
        return true;
    }

    private async Task<Process> CreateClientProcess(ClientProfile profile, IEnumerable<string> modList, bool isBattleEye, bool autoConnect)
    {
        var filename = _setup.GetBinFile(isBattleEye);
        var modlistFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(modlistFile, _setup.GetModsPath(modList));
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

    private async Task<Process> CreateServerProcess(int instance, ServerProfile profile, IEnumerable<string> modlist)
    {
        var process = new Process();

        var filename = _setup.GetIntanceBinary(instance);
        
        var modfileFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(modfileFile, _setup.GetModsPath(modlist));
        
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
    
    private async Task FindExistingClient()
    {
        if (_conanClientProcess != null) return;

        var process = await FindClientProcess();
        if (process is not null)
        {
            _conanClientProcess = await _processFactory.Create()
                .SetStartDate(process.Start)
                .SetProcess(process.Process)
                .BuildClient();
            OnStateChanged();
        }
    }

    private async Task FindExistingServers()
    {
        await foreach (var process in FindServerProcesses())
        {
            if(_serverProcesses.ContainsKey(process.Instance)) continue;
            var serverInfos = await _iniHandler.GetInfosFromServerAsync(process.Instance);
            var builder = _processFactory.Create()
                .SetStartDate(process.Start)
                .SetProcess(process.Process)
                .SetServerInfos(serverInfos)
                .SetLogFile(process.GameLogs);
            if (serverInfos.RConPort > 0)
                builder.UseRCon();
            _serverProcesses.TryAdd(process.Instance, await builder.BuildServer());
            OnStateChanged();
        }
    }

    private async Task CleanStoppedProcesses()
    {
        if (_conanClientProcess != null && !_conanClientProcess.State.IsRunning())
        {
            _logger.LogInformation("Client stopped");
            _conanClientProcess.Dispose();
            _conanClientProcess = null;
            OnStateChanged();
        }

        foreach (var server in _serverProcesses.ToList())
        {
            if (!server.Value.State.IsRunning())
            {
                _logger.LogInformation("Server {instance} stopped", server.Key);
                _serverProcesses.Remove(server.Key);
                OnStateChanged();
                var name = _appFiles.Server.Resolve(_setup.Config.GetInstanceProfile(server.Key));
                if ((server.Value.State == ProcessState.CRASHED && _appFiles.Server.Get(name).RestartWhenDown) 
                    || server.Value.State == ProcessState.NEW)
                {
                    await CatapultServer(server.Key);
                }
                server.Value.Dispose();
            }
        }
    }

    private bool IsClientProfileLocked(ClientProfileRef profileRef)
    {
        if (_conanClientProcess == null) return false;
        var junction = Path.GetFullPath(GetCurrentClientJunction());
        var profilePath = Path.GetFullPath(_appFiles.Client.GetDirectory(profileRef));
        return string.Equals(junction, profilePath, StringComparison.Ordinal);
    }

    private bool IsServerProfileLocked(ServerProfileRef profileRef)
    {
        var profilePath = Path.GetFullPath(_appFiles.Server.GetDirectory(profileRef));
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
        var path = Path.Combine(_setup.GetClientFolder(), Constants.FolderGameSave);
        if (JunctionPoint.Exists(path))
            return JunctionPoint.GetTarget(path);
        return string.Empty;
    }

    private string GetCurrentServerJunction(int instance)
    {
        var path = Path.Combine(_setup.GetInstancePath(instance), Constants.FolderGameSave);
        if (JunctionPoint.Exists(path))
            return JunctionPoint.GetTarget(path);
        return string.Empty;
    }

    private void SetupJunction(string junction, string targetPath)
    {
        _logger.LogInformation("Setup new junction {junction} > {target}", junction, targetPath);
        Tools.RemoveSymboliclink(junction);
        Tools.SetupSymboliclink(junction, targetPath);
    }

    private void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}