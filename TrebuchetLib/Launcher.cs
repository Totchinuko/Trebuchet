using SteamKit2.GC.Dota.Internal;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Trebuchet;

namespace TrebuchetLib
{
    public class Launcher
    {
        private static Regex _instanceRegex = new Regex(@"-TotInstance=([0-9+])");

        private ClientProcess? _clientProcess;

        private object _lock = new object();
        private HashSet<string> _lockedFolders = new HashSet<string>();

        private Dictionary<int, ServerProcess> _serverProcesses = new Dictionary<int, ServerProcess>();

        private BlockingCollection<Action> queue = new BlockingCollection<Action>(new ConcurrentQueue<Action>());

        public Launcher(Config config)
        {
            Config = config;

            Task.Run(Tick);
        }

        public event EventHandler<TrebuchetFailEventArgs>? ClientFailed;

        public event EventHandler<TrebuchetStartEventArgs>? ClientStarted;

        public event EventHandler? ClientTerminated;

        public event EventHandler<TrebuchetFailEventArgs>? ServerFailed;

        public event EventHandler<TrebuchetStartEventArgs>? ServerStarted;

        public event EventHandler<int>? ServerTerminated;

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
                _clientProcess.ProcessExited += OnClientProcessTerminate;
                _clientProcess.ProcessStarted += OnClientProcessStarted;
                _clientProcess.ProcessFailed += OnClientProcessFailed;

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
                watcher.ProcessExited += OnServerProcessTerminate;
                watcher.ProcessStarted += OnServerProcessStarted;
                watcher.ProcessFailed += OnServerProcessFailed;
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
            Invoke(() =>
            {
                if (_serverProcesses.TryGetValue(instance, out var watcher))
                    watcher.Close();
            });
        }

        /// <summary>
        /// Get the server port informations for all the running server processes.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ServerInstanceInformation> GetInstancesInformations()
        {
            lock (_lock)
            {
                foreach (ServerProcess p in _serverProcesses.Values)
                    yield return p.Information;
            }
        }

        /// <summary>
        /// Kill the client process.
        /// </summary>
        public void KillClient()
        {
            Invoke(() =>
            {
                if (_clientProcess == null) return;
                Log.Write("Requesting client process kill", LogSeverity.Info);
                _clientProcess.Kill();
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
                    Log.Write($"Requesting server process kill on instance {instance}", LogSeverity.Info);
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
            if (_clientProcess != null) return;

            var data = Tools.GetProcessesWithName(Config.FileClientBin).FirstOrDefault();
            if (data.IsEmpty) return;
            if (!TrebuchetLaunch.TryLoadPreviousLaunch(Config, out ClientProfile? profile, out ModListProfile? modlist)) return;
            if (!data.TryGetProcess(out Process? process)) return;

            _clientProcess = new ClientProcess(profile, modlist, false, process, data);
            _clientProcess.ProcessExited += OnClientProcessTerminate;
            _lockedFolders.Add(GetCurrentClientJunction());
            OnClientProcessStarted(this, new TrebuchetStartEventArgs(data));
        }

        private void FindExistingServers()
        {
            var processes = Tools.GetProcessesWithName(Config.FileServerBin);
            foreach (var p in processes)
            {
                if (!GetInstance(p.args, out int instance)) continue;
                if (_serverProcesses.ContainsKey(instance)) continue;
                if (!p.TryGetProcess(out Process? process)) continue;
                if (!TrebuchetLaunch.TryLoadPreviousLaunch(Config, instance, out ServerProfile? profile, out ModListProfile? modlist)) continue;

                var watcher = new ServerProcess(profile, modlist, instance, process, p);
                watcher.ProcessExited += OnServerProcessTerminate;
                _serverProcesses.Add(instance, watcher);
                _lockedFolders.Add(GetCurrentServerJunction(instance));
                OnServerProcessStarted(this, new TrebuchetStartEventArgs(p, instance));
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

        private void Invoke(Action callback)
        {
            queue.Add(callback);
        }

        private bool IsRestartWhenDown(string profileName)
        {
            if (!ServerProfile.TryLoadProfile(Config, profileName, out ServerProfile? profile)) return false;
            return profile.RestartWhenDown;
        }

        private void OnClientProcessFailed(object? sender, TrebuchetFailEventArgs e)
        {
            Log.Write($"Client process failed to start", LogSeverity.Error);
            Log.Write(e.Exception);
            ClientFailed?.Invoke(this, e);
        }

        private void OnClientProcessStarted(object? sender, TrebuchetStartEventArgs e)
        {
            Log.Write($"Client process started ({e.process.pid})", LogSeverity.Info);
            ClientStarted?.Invoke(this, e);
        }

        private void OnClientProcessTerminate(object? sender, EventArgs e)
        {
            if (sender is not ClientProcess) return;
            Log.Write($"Client process terminated", LogSeverity.Info);

            ClientTerminated?.Invoke(this, EventArgs.Empty);

            Invoke(() =>
            {
                _clientProcess = null;
                _lockedFolders.Remove(GetCurrentClientJunction());
            });
        }

        private void OnServerProcessFailed(object? sender, TrebuchetFailEventArgs e)
        {
            Log.Write($"Server process failed to start", LogSeverity.Error);
            Log.Write(e.Exception);
            ServerFailed?.Invoke(this, e);
        }

        private void OnServerProcessStarted(object? sender, TrebuchetStartEventArgs e)
        {
            Log.Write($"Server instance {e.instance} started ({e.process.pid})", LogSeverity.Info);
            ServerStarted?.Invoke(this, e);
        }

        private void OnServerProcessTerminate(object? sender, EventArgs e)
        {
            if (sender is not ServerProcess process) return;

            ServerTerminated?.Invoke(this, process.ServerInstance);

            Invoke(() =>
            {
                _serverProcesses.Remove(process.ServerInstance);
                _lockedFolders.Remove(GetCurrentServerJunction(process.ServerInstance));
                Log.Write($"Server intance {process.ServerInstance} terminated", LogSeverity.Info);

                if (!process.Closed && IsRestartWhenDown(process.Profile.ProfileName))
                {
                    Log.Write($"Restarting server instance {process.ServerInstance}", LogSeverity.Info);
                    CatapultServer(process.Profile.ProfileName, process.Modlist.ProfileName, process.ServerInstance);
                }
            });
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
            while (true)
            {
                lock (_lock)
                {
                    foreach (var q in queue)
                        q.Invoke();

                    if (DateTime.UtcNow - lastSearch > TimeSpan.FromSeconds(1))
                    {
                        lastSearch = DateTime.UtcNow;
                        FindExistingClient();
                        FindExistingServers();
                    }
                }
            }
        }
    }
}