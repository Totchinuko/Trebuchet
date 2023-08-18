using Goog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GoogLib
{
    public sealed class Trebuchet
    {
        private static Regex _instanceRegex = new Regex(@"-TotInstance=([0-9+])");
        private ClientProcess? _clientProcess;
        private Config _config;
        private HashSet<string> _lockedFolders = new HashSet<string>();
        private Dictionary<int, ServerProcess> _serverProcesses = new Dictionary<int, ServerProcess>();

        public Trebuchet(Config config)
        {
            _config = config;
        }

        public event EventHandler<TrebuchetFailEventArgs>? ClientFailed;

        public event EventHandler<TrebuchetStartEventArgs>? ClientStarted;

        public event EventHandler? ClientTerminated;

        public event EventHandler<Action>? DispatcherRequest;

        public event EventHandler<TrebuchetFailEventArgs>? ServerFailed;

        public event EventHandler<TrebuchetStartEventArgs>? ServerStarted;

        public event EventHandler<int>? ServerTerminated;

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
            if (_clientProcess != null) return;

            if (!ClientProfile.TryLoadProfile(_config, profileName, out ClientProfile? profile))
                throw new FileNotFoundException($"{profileName} profile not found.");
            if (!ModListProfile.TryLoadProfile(_config, modlistName, out ModListProfile? modlist))
                throw new FileNotFoundException($"{modlistName} modlist not found.");

            if (_lockedFolders.Contains(profile.ProfileFolder))
                throw new ArgumentException($"Profile {profileName} folder is currently locked by another process.");

            SetupJunction(_config.ClientPath, profile.ProfileFolder);

            _clientProcess = new ClientProcess(profile, modlist, isBattleEye);
            _clientProcess.ProcessExited += OnClientProcessTerminate;
            _clientProcess.ProcessStarted += OnClientProcessStarted;
            _clientProcess.ProcessFailed += OnClientProcessFailed;

            _lockedFolders.Add(GetCurrentClientJunction());
            Task.Run(_clientProcess.StartProcessAsync);
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
            if (_serverProcesses.ContainsKey(instance)) return;

            if (!ServerProfile.TryLoadProfile(_config, profileName, out ServerProfile? profile))
                throw new FileNotFoundException($"{profileName} profile not found.");
            if (!ModListProfile.TryLoadProfile(_config, modlistName, out ModListProfile? modlist))
                throw new FileNotFoundException($"{modlistName} modlist not found.");

            if (_lockedFolders.Contains(profile.ProfileFolder))
                throw new ArgumentException($"Profile {profileName} folder is currently locked by another process.");

            SetupJunction(ServerProfile.GetInstancePath(_config, instance), profile.ProfileFolder);

            ServerProcess watcher = new ServerProcess(profile, modlist, instance);
            watcher.ProcessExited += OnServerProcessTerminate;
            watcher.ProcessStarted += OnServerProcessStarted;
            watcher.ProcessFailed += OnServerProcessFailed;
            _serverProcesses.Add(instance, watcher);

            _lockedFolders.Add(GetCurrentServerJunction(instance));
            Task.Run(watcher.StartProcessAsync);
        }

        /// <summary>
        /// Ask a particular server instance to close. If the process is borked, this will not work.
        /// </summary>
        /// <param name="instance"></param>
        public void CloseServer(int instance)
        {
            if (_serverProcesses.TryGetValue(instance, out var watcher))
                watcher.Close();
        }

        public bool IsAnyServerRunning() => _serverProcesses.Count > 0;

        public bool IsClientRunning() => _clientProcess != null && _clientProcess.IsRunning;

        public bool IsFolderLocked(string path) => _lockedFolders.Where(path.StartsWith).Any();

        public bool IsServerRunning(int instance) => _serverProcesses.TryGetValue(instance, out var watcher) && watcher.IsRunning;

        /// <summary>
        /// Terminate all active server processes.
        /// </summary>
        public void KillAllServers()
        {
            foreach (ServerProcess p in _serverProcesses.Values)
                p.Kill();
        }

        /// <summary>
        /// Kill the client process.
        /// </summary>
        public void KillClient() => _clientProcess?.Kill();

        /// <summary>
        /// Kill a particular server instance.
        /// </summary>
        /// <param name="instance"></param>
        public void KillServer(int instance)
        {
            if (_serverProcesses.TryGetValue(instance, out var watcher))
                watcher.Kill();
        }

        /// <summary>
        /// Request a gracefull shutdown of all active server processes.
        /// </summary>
        public void StopAllServers()
        {
            foreach (ServerProcess p in _serverProcesses.Values)
                p.Close();
        }

        /// <summary>
        /// Tick the trebuchet. This will check if client or any server is already running and catch them if they where started by trebuchet. It also check for zombie processes.
        /// </summary>
        public void TickTrebuchet()
        {
            FindExistingClient();
            FindExistingServers();

            foreach (ServerProcess server in _serverProcesses.Values)
            {
                server.ProcessRefresh();
                if ((server.LastResponsive + TimeSpan.FromSeconds(_config.ZombieCheckSeconds)) < DateTime.UtcNow && _config.KillZombies)
                    server.Kill();
            }
        }

        private static bool GetInstance(string path, out int instance)
        {
            instance = 0;
            var match = _instanceRegex.Match(path);
            if (!match.Success) return false;

            return int.TryParse(match.Groups[1].Value, out instance);
        }

        private void AddDispatch(Action callback)
        {
            DispatcherRequest?.Invoke(this, callback);
        }

        private void FindExistingClient()
        {
            if (_clientProcess != null) return;

            var data = Tools.GetProcessesWithName(Config.FileClientBin).FirstOrDefault();
            if (data.IsEmpty) return;
            if (!TrebuchetLaunch.TryLoadPreviousLaunch(_config, out ClientProfile? profile, out ModListProfile? modlist)) return;
            if (!data.TryGetProcess(out Process? process)) return;

            _clientProcess = new ClientProcess(profile, modlist, false);
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
                if (!TrebuchetLaunch.TryLoadPreviousLaunch(_config, instance, out ServerProfile? profile, out ModListProfile? modlist)) continue;

                var watcher = new ServerProcess(profile, modlist, instance, process, p);
                watcher.ProcessExited += OnServerProcessTerminate;
                _serverProcesses.Add(instance, watcher);
                _lockedFolders.Add(GetCurrentServerJunction(instance));
                OnServerProcessStarted(this, new TrebuchetStartEventArgs(p, instance));
            }
        }

        private string GetCurrentClientJunction()
        {
            string path = Path.Combine(_config.ClientPath, Config.FolderGameSave);
            if (JunctionPoint.Exists(path))
                return JunctionPoint.GetTarget(path);
            else return string.Empty;
        }

        private string GetCurrentServerJunction(int instance)
        {
            string path = Path.Combine(ServerProfile.GetInstancePath(_config, instance), Config.FolderGameSave);
            if (JunctionPoint.Exists(path))
                return JunctionPoint.GetTarget(path);
            else return string.Empty;
        }

        private void OnClientProcessFailed(object? sender, TrebuchetFailEventArgs e)
        {
            AddDispatch(() =>
            {
                ClientFailed?.Invoke(this, e);
            });
        }

        private void OnClientProcessStarted(object? sender, TrebuchetStartEventArgs e)
        {
            AddDispatch(() =>
            {
                ClientStarted?.Invoke(this, e);
            });
        }

        private void OnClientProcessTerminate(object? sender, EventArgs e)
        {
            AddDispatch(() =>
            {
                if (sender is not ClientProcess) return;
                _clientProcess = null;
                _lockedFolders.Remove(GetCurrentClientJunction());
                ClientTerminated?.Invoke(this, EventArgs.Empty);
            });
        }

        private void OnServerProcessFailed(object? sender, TrebuchetFailEventArgs e)
        {
            AddDispatch(() =>
            {
                ServerFailed?.Invoke(this, e);
            });
        }

        private void OnServerProcessStarted(object? sender, TrebuchetStartEventArgs e)
        {
            AddDispatch(() =>
            {
                ServerStarted?.Invoke(this, e);
            });
        }

        private void OnServerProcessTerminate(object? sender, EventArgs e)
        {
            AddDispatch(() =>
            {
                if (sender is not ServerProcess process) return;
                if (!_serverProcesses.ContainsKey(process.ServerInstance)) return;
                
                _serverProcesses.Remove(process.ServerInstance);
                _lockedFolders.Remove(GetCurrentServerJunction(process.ServerInstance));

                if (!process.Closed && _config.RestartWhenDown)
                    CatapultServer(process.Profile.ProfileName, process.Modlist.ProfileName, process.ServerInstance);
                else
                    ServerTerminated?.Invoke(this, process.ServerInstance);
            });
        }

        private void SetupJunction(string gamePath, string targetPath)
        {
            string junction = Path.Combine(gamePath, Config.FolderGameSave);
            Tools.RemoveSymboliclink(junction);
            Tools.SetupSymboliclink(junction, targetPath);
        }
    }
}