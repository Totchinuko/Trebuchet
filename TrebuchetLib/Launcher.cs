using SteamKit2.GC.Dota.Internal;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Trebuchet;

namespace TrebuchetLib
{
    public class Launcher : IDisposable
    {
        private static Regex _instanceRegex = new Regex(@"-TotInstance=([0-9+])");
        private ClientProcess? _clientProcess;
        private bool _isRunning = true;
        private object _lock = new object();
        private HashSet<string> _lockedFolders = new HashSet<string>();
        private Dictionary<int, ServerProcess> _serverProcesses = new Dictionary<int, ServerProcess>();

        public Launcher(Config config)
        {
            Config = config;

            Task.Run(Tick);
        }

        public event EventHandler<ProcessDetailsEventArgs>? ClientProcessStateChanged;

        public event EventHandler<ProcessServerDetailsEventArgs>? ServerProcessStateChanged;

        public Config Config { get; }

        /// <summary>
        /// Launch a client process while taking care of everything. Generate the modlist, generate the ini settings, etc.
        /// Process is created on a separate thread, and fire the event ClientProcessStarted when the process is running.
        /// </summary>
        /// <param name="profileName"></param>
        /// <param name="modlistName"></param>
        /// <param name="isBattleEye">Launch with BattlEye anti cheat.</param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ArgumentException">Profiles can only be used by one process at a times, since they contain the db of the game.</exception>
        public void CatapultClient(string profileName, string modlistName, bool isBattleEye)
        {
            lock (_lock)
            {
                if (_clientProcess != null) return;

                if (!ClientProfile.TryLoadProfile(Config, profileName, out ClientProfile? profile))
                    throw new FileNotFoundException($"{profileName} profile not found.");
                if (!ModListProfile.TryLoadProfile(Config, modlistName, out ModListProfile? modlist))
                    throw new FileNotFoundException($"{modlistName} modlist not found.");

                if (_lockedFolders.Contains(profile.ProfileFolder))
                    throw new ArgumentException($"Profile {profileName} folder is currently locked by another process.");

                SetupJunction(Config.ClientPath, profile.ProfileFolder);

                _clientProcess = new ClientProcess(profile, modlist, isBattleEye);
                _clientProcess.ProcessStateChanged += OnClientProcessStateChanged;

                _lockedFolders.Add(GetCurrentClientJunction());
                Log.Write($"Locking folder {profile.ProfileName}", LogSeverity.Debug);
                Log.Write($"Launching client process with profile {profileName} and modlist {modlistName}", LogSeverity.Info);
                Task.Run(_clientProcess.StartProcessAsync);
            }
        }

        /// <summary>
        /// Launch a server process while taking care of everything. Generate the modlist, generate the ini settings, etc.
        /// Process is created on a separate thread, and fire the event ServerProcessStarted when the process is running.
        /// </summary>
        /// <param name="profileName"></param>
        /// <param name="modlistName"></param>
        /// <param name="instance">Index of the instance you want to launch</param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ArgumentException">Profiles can only be used by one process at a times, since they contain the db of the game.</exception>
        public void CatapultServer(string profileName, string modlistName, int instance)
        {
            lock (_lock)
            {
                if (_serverProcesses.ContainsKey(instance)) return;

                if (!ServerProfile.TryLoadProfile(Config, profileName, out ServerProfile? profile))
                    throw new FileNotFoundException($"{profileName} profile not found.");
                if (!ModListProfile.TryLoadProfile(Config, modlistName, out ModListProfile? modlist))
                    throw new FileNotFoundException($"{modlistName} modlist not found.");

                if (_lockedFolders.Contains(profile.ProfileFolder))
                    throw new ArgumentException($"Profile {profileName} folder is currently locked by another process.");

                SetupJunction(ServerProfile.GetInstancePath(Config, instance), profile.ProfileFolder);

                ServerProcess watcher = new ServerProcess(profile, modlist, instance);
                watcher.ProcessStateChanged += OnServerProcessStateChanged;
                _serverProcesses.Add(instance, watcher);

                _lockedFolders.Add(GetCurrentServerJunction(instance));
                Log.Write($"Locking folder {profile.ProfileName}", LogSeverity.Debug);
                Log.Write($"Launching server process with profile {profileName} and modlist {modlistName} on instance {instance}", LogSeverity.Info);
                Task.Run(watcher.StartProcessAsync);
            }
        }

        /// <summary>
        /// Ask a particular server instance to close. If the process is borked, this will not work.
        /// </summary>
        /// <param name="instance"></param>
        public void CloseServer(int instance)
        {
            Log.Write($"Requesting server instance {instance} stop", LogSeverity.Info);
            lock (_lock)
            {
                if (_serverProcesses.TryGetValue(instance, out var watcher))
                    watcher.Close();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _isRunning = false;
                foreach (var item in _serverProcesses)
                    item.Value.Dispose();
                _serverProcesses.Clear();
            }
        }

        public ProcessDetails? GetClientDetails()
        {
            return _clientProcess?.ProcessDetails;
        }

        public IConsole GetServerConsole(int instance)
        {
            lock (_lock)
            {
                if (_serverProcesses.TryGetValue(instance, out var watcher))
                    return watcher.Console;
                throw new ArgumentException($"Server instance {instance} is not running.");
            }
        }

        public IRcon GetServerRcon(int instance)
        {
            lock (_lock)
            {
                if (_serverProcesses.TryGetValue(instance, out var watcher))
                    return watcher.Rcon;
                throw new ArgumentException($"Server instance {instance} is not running.");
            }
        }

        /// <summary>
        /// Get the server port informations for all the running server processes.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ProcessServerDetails> GetServersDetails()
        {
            lock (_lock)
            {
                foreach (ServerProcess p in _serverProcesses.Values)
                    yield return p.ProcessDetails;
            }
        }

        public bool IsAnyServerRunning()
        {
            lock (_lock)
            {
                return _serverProcesses.Count > 0;
            }
        }

        public bool IsClientRunning()
        {
            lock (_lock)
            {
                return _clientProcess != null;
            }
        }

        /// <summary>
        /// Kill the client process.
        /// </summary>
        public void KillClient()
        {
            lock (_lock)
            {
                if (_clientProcess == null) return;
                Log.Write("Requesting client process kill", LogSeverity.Info);
                _clientProcess.Kill();
            }
        }

        /// <summary>
        /// Kill a particular server instance.
        /// </summary>
        /// <param name="instance"></param>
        public void KillServer(int instance)
        {
            lock (_lock)
            {
                if (_serverProcesses.TryGetValue(instance, out var watcher))
                {
                    Log.Write($"Requesting server process kill on instance {instance}", LogSeverity.Info);
                    watcher.Kill();
                }
            }
        }

        private static bool GetInstance(string path, out int instance)
        {
            instance = 0;
            var match = _instanceRegex.Match(path);
            if (!match.Success) return false;

            return int.TryParse(match.Groups[1].Value, out instance);
        }

        private void FindExistingClient()
        {
            var data = Tools.GetProcessesWithName(Config.FileClientBin).FirstOrDefault();

            ClientProcess client;
            Process? process;
            lock (_lock)
            {
                if (_clientProcess != null) return;
                if (data.IsEmpty) return;
                if (!TrebuchetLaunch.TryLoadPreviousLaunch(Config, out ClientProfile? profile, out ModListProfile? modlist)) return;
                if (!data.TryGetProcess(out process)) return;

                client = new ClientProcess(profile, modlist, false);
                client.ProcessStateChanged += OnClientProcessStateChanged;
                _clientProcess = client;
                _lockedFolders.Add(GetCurrentClientJunction());
            }
            client.SetExistingProcess(process, data);
        }

        private void FindExistingServers()
        {
            var processes = Tools.GetProcessesWithName(Config.FileServerBin);
            foreach (var p in processes)
            {
                int instance;
                ServerProcess server;
                Process? process;
                lock (_lock)
                {
                    if (!GetInstance(p.args, out instance)) continue;
                    if (_serverProcesses.ContainsKey(instance)) continue;
                    if (!p.TryGetProcess(out process)) continue;
                    if (!TrebuchetLaunch.TryLoadPreviousLaunch(Config, instance, out ServerProfile? profile, out ModListProfile? modlist)) continue;

                    server = new ServerProcess(profile, modlist, instance);
                    server.ProcessStateChanged += OnServerProcessStateChanged;
                    _serverProcesses.Add(instance, server);
                    _lockedFolders.Add(GetCurrentServerJunction(instance));
                }
                server.SetExistingProcess(process, p);
            }
        }

        private string GetCurrentClientJunction()
        {
            string path = Path.Combine(Config.ClientPath, Config.FolderGameSave);
            if (JunctionPoint.Exists(path))
                return JunctionPoint.GetTarget(path);
            else return string.Empty;
        }

        private string GetCurrentServerJunction(int instance)
        {
            string path = Path.Combine(ServerProfile.GetInstancePath(Config, instance), Config.FolderGameSave);
            if (JunctionPoint.Exists(path))
                return JunctionPoint.GetTarget(path);
            else return string.Empty;
        }

        private bool IsRestartWhenDown(string profileName)
        {
            if (!ServerProfile.TryLoadProfile(Config, profileName, out ServerProfile? profile)) return false;
            return profile.RestartWhenDown;
        }

        private void OnClientProcessStateChanged(object? sender, ProcessDetailsEventArgs e)
        {
            ClientProcessStateChanged?.Invoke(this, e);

            if (e.NewDetails.State != ProcessState.CRASHED || e.NewDetails.State != ProcessState.STOPPED) return;

            lock (_lock)
            {
                _clientProcess = null;
                _lockedFolders.Remove(GetCurrentClientJunction());
            }
        }

        private void OnServerProcessStateChanged(object? sender, ProcessServerDetailsEventArgs e)
        {
            ServerProcessStateChanged?.Invoke(this, e);
            if (e.NewDetails.State != ProcessState.STOPPED || e.NewDetails.State != ProcessState.CRASHED) return;
            lock (_lock)
            {
                _serverProcesses.Remove(e.NewDetails.Instance);
                _lockedFolders.Remove(GetCurrentServerJunction(e.NewDetails.Instance));
                Log.Write($"Server intance {e.NewDetails.Instance} terminated", LogSeverity.Info);

                if (e.NewDetails.State == ProcessState.CRASHED && IsRestartWhenDown(e.NewDetails.Profile))
                {
                    Log.Write($"Restarting server instance {e.NewDetails.Instance}", LogSeverity.Info);
                    CatapultServer(e.NewDetails.Profile, e.NewDetails.Modlist, e.NewDetails.Instance);
                }
            }
        }

        private void SetupJunction(string gamePath, string targetPath)
        {
            string junction = Path.Combine(gamePath, Config.FolderGameSave);
            Tools.RemoveSymboliclink(junction);
            Tools.SetupSymboliclink(junction, targetPath);
        }

        /// <summary>
        /// Tick the trebuchet. This will check if client or any server is already running and catch them if they where started by trebuchet.
        /// </summary>
        private void Tick()
        {
            DateTime lastSearch = DateTime.UtcNow;
            while (_isRunning)
            {
                if (DateTime.UtcNow - lastSearch > TimeSpan.FromSeconds(1))
                {
                    lastSearch = DateTime.UtcNow;
                    FindExistingClient();
                    FindExistingServers();
                }

                Thread.Sleep(100);
            }
        }
    }
}