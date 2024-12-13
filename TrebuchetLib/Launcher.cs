using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Serilog;
using Trebuchet;
using TrebuchetUtils;

namespace TrebuchetLib
{
    public class Launcher : IDisposable
    {
        private static Regex _instanceRegex = new Regex(@"-TotInstance=([0-9+])");
        private ClientProcess? _clientProcess;
        private bool _isRunning = true;
        private object _lock = new object();
        private HashSet<string> _lockedFolders = new HashSet<string>();
        private ConcurrentDictionary<int, ServerProcess> _serverProcesses = new ConcurrentDictionary<int, ServerProcess>();
        private ConcurrentQueue<Action> _threadQueue = new ConcurrentQueue<Action>();

        public Launcher(Config config)
        {
            Config = config;

            Task.Run(ThreadLoop);
        }

        public event EventHandler<ProcessDetailsEventArgs>? ClientProcessStateChanged;

        public event EventHandler<ProcessServerDetailsEventArgs>? ServerProcessStateChanged;

        public Config Config { get; }

        private ClientProcess? ClientProcess
        {
            get
            {
                lock (_lock)
                    return _clientProcess;
            }
            set
            {
                lock (_lock)
                    _clientProcess = value;
            }
        }

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
            Invoke(() =>
            {
                if (ClientProcess != null) return;

                if (!ClientProfile.TryLoadProfile(Config, profileName, out ClientProfile? profile))
                    throw new FileNotFoundException($"{profileName} profile not found.");
                if (!ModListProfile.TryLoadProfile(Config, modlistName, out ModListProfile? modlist))
                    throw new FileNotFoundException($"{modlistName} modlist not found.");

                if (_lockedFolders.Contains(profile.ProfileFolder))
                    throw new ArgumentException($"Profile {profileName} folder is currently locked by another process.");

                SetupJunction(Config.ClientPath, profile.ProfileFolder);

                ClientProcess = new ClientProcess(profile, modlist, isBattleEye);
                ClientProcess.ProcessStateChanged += OnClientProcessStateChanged;

                _lockedFolders.Add(GetCurrentClientJunction());
                Log.Debug($"Locking folder {profile.ProfileName}");
                Log.Information($"Launching client process with profile {profileName} and modlist {modlistName}");
                Task.Run(ClientProcess.StartProcessAsync);
            });
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
            Invoke(() =>
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
                _serverProcesses.TryAdd(instance, watcher);

                _lockedFolders.Add(GetCurrentServerJunction(instance));
                Log.Debug($"Locking folder {profile.ProfileName}");
                Log.Information($"Launching server process with profile {profileName} and modlist {modlistName} on instance {instance}");
                Task.Run(watcher.StartProcessAsync);
            });
        }

        /// <summary>
        /// Ask a particular server instance to close. If the process is borked, this will not work.
        /// </summary>
        /// <param name="instance"></param>
        public void CloseServer(int instance)
        {
            Log.Information($"Requesting server instance {instance} stop");
            Invoke(() =>
            {
                if (_serverProcesses.TryGetValue(instance, out var watcher))
                    watcher.Close();
            });
        }

        public void Dispose()
        {
            Invoke(() =>
            {
                _isRunning = false;
                foreach (var item in _serverProcesses)
                    item.Value.Dispose();
                _serverProcesses.Clear();
            });
        }

        public ProcessDetails? GetClientDetails()
        {
            return ClientProcess?.ProcessDetails;
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
                return watcher.Rcon;
            throw new ArgumentException($"Server instance {instance} is not running.");
        }

        /// <summary>
        /// Get the server port informations for all the running server processes.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ProcessServerDetails> GetServersDetails()
        {
            foreach (ServerProcess p in _serverProcesses.Values)
                yield return p.ProcessDetails;
        }

        public void Invoke(Action action)
        {
            _threadQueue.Enqueue(action);
        }

        public bool IsAnyServerRunning()
        {
            return _serverProcesses.Count > 0;
        }

        public bool IsClientRunning()
        {
            return ClientProcess != null;
        }

        /// <summary>
        /// Kill the client process.
        /// </summary>
        public void KillClient()
        {
            Invoke(() =>
            {
                if (ClientProcess == null) return;
                Log.Information("Requesting client process kill");
                ClientProcess.Kill();
            });
        }

        /// <summary>
        /// Kill a particular server instance.
        /// </summary>
        /// <param name="instance"></param>
        public void KillServer(int instance)
        {
            Invoke(() =>
            {
                if (_serverProcesses.TryGetValue(instance, out var watcher))
                {
                    Log.Information($"Requesting server process kill on instance {instance}");
                    watcher.Kill();
                }
            });
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
            var data = Trebuchet.Tools.GetProcessesWithName(Config.FileClientBin).FirstOrDefault();

            if (ClientProcess != null) return;
            if (data.IsEmpty) return;
            if (!TrebuchetLaunch.TryLoadPreviousLaunch(Config, out ClientProfile? profile, out ModListProfile? modlist)) return;
            if (!data.TryGetProcess(out Process? process)) return;

            ClientProcess client = new ClientProcess(profile, modlist, false);
            client.ProcessStateChanged += OnClientProcessStateChanged;
            ClientProcess = client;
            _lockedFolders.Add(GetCurrentClientJunction());
            client.SetExistingProcess(process, data);
        }

        private void FindExistingServers()
        {
            var processes = Trebuchet.Tools.GetProcessesWithName(Config.FileServerBin);
            foreach (var p in processes)
            {
                if (!GetInstance(p.args, out int instance)) continue;
                if (_serverProcesses.ContainsKey(instance)) continue;
                if (!p.TryGetProcess(out Process? process)) continue;
                if (!TrebuchetLaunch.TryLoadPreviousLaunch(Config, instance, out ServerProfile? profile, out ModListProfile? modlist)) continue;

                ServerProcess server = new ServerProcess(profile, modlist, instance);
                server.ProcessStateChanged += OnServerProcessStateChanged;
                _serverProcesses.TryAdd(instance, server);
                _lockedFolders.Add(GetCurrentServerJunction(instance));
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
            if (e.NewDetails.State.IsRunning()) return;

            Invoke(() =>
            {
                ClientProcess = null;
                _lockedFolders.Remove(GetCurrentClientJunction());
            });
        }

        private void OnServerProcessStateChanged(object? sender, ProcessServerDetailsEventArgs e)
        {
            ServerProcessStateChanged?.Invoke(this, e);
            if (e.NewDetails.State.IsRunning()) return;
            Invoke(() =>
            {
                _serverProcesses.TryRemove(e.NewDetails.Instance, out _);
                _lockedFolders.Remove(GetCurrentServerJunction(e.NewDetails.Instance));
                Log.Information($"Server intance {e.NewDetails.Instance} terminated");

                if (e.NewDetails.State == ProcessState.CRASHED && IsRestartWhenDown(e.NewDetails.Profile))
                {
                    Log.Information($"Restarting server instance {e.NewDetails.Instance}");
                    CatapultServer(e.NewDetails.Profile, e.NewDetails.Modlist, e.NewDetails.Instance);
                }
            });
        }

        private void SetupJunction(string gamePath, string targetPath)
        {
            string junction = Path.Combine(gamePath, Config.FolderGameSave);
            Trebuchet.Tools.RemoveSymboliclink(junction);
            Trebuchet.Tools.SetupSymboliclink(junction, targetPath);
        }

        /// <summary>
        /// Tick the trebuchet. This will check if client or any server is already running and catch them if they where started by trebuchet.
        /// </summary>
        private void ThreadLoop()
        {
            DateTime lastSearch = DateTime.UtcNow;
            while (_isRunning)
            {
                while (_threadQueue.TryDequeue(out Action? action))
                    action();

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