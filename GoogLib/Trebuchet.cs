using Goog;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        public event EventHandler<TrebuchetStartEventArgs>? ClientProcessStarted;

        public event EventHandler? ClientTerminated;

        public event EventHandler<Action>? DispatcherRequest;

        public event EventHandler<TrebuchetStartEventArgs>? ServerProcessStarted;

        public event EventHandler<int>? ServerTerminated;

        public static bool GetInstance(string path, out int instance)
        {
            instance = 0;
            var match = _instanceRegex.Match(path);
            if (!match.Success) return false;

            return int.TryParse(match.Groups[1].Value, out instance);
        }

        public void CatapultClient(string profileName, string modlistName, bool isBattleEye)
        {
            if (_clientProcess != null)
                throw new Exception("Client is already running.");

            if (!ClientProfile.TryLoadProfile(_config, profileName, out ClientProfile? profile))
                throw new Exception("Unknown Profile.");
            if (!ModListProfile.TryLoadProfile(_config, modlistName, out ModListProfile? modlist))
                throw new Exception("Unknown Mod List.");

            if (_lockedFolders.Contains(profile.ProfileFolder))
                throw new Exception("Profile folder is currently locked by another process.");

            SetupJunction(_config.ClientPath, profile.ProfileFolder);

            _clientProcess = new ClientProcess(profile, modlist, isBattleEye);
            _clientProcess.ProcessExited += OnClientProcessTerminate;
            _clientProcess.ProcessStarted += OnClientProcessStarted;

            _lockedFolders.Add(GetCurrentClientJunction());
            Task.Run(_clientProcess.StartProcessAsync);
        }

        public void CatapultServer(string profileName, string modlistName, int instance)
        {
            if (_serverProcesses.ContainsKey(instance)) return;

            if (!ServerProfile.TryLoadProfile(_config, profileName, out ServerProfile? profile))
                throw new Exception("Unknown Profile.");
            if (!ModListProfile.TryLoadProfile(_config, modlistName, out ModListProfile? modlist))
                throw new Exception("Unknown Mod List.");

            if (_lockedFolders.Contains(profile.ProfileFolder))
                throw new Exception("Profile folder is currently locked by another process.");

            SetupJunction(ServerProfile.GetInstancePath(_config, instance), profile.ProfileFolder);

            ServerProcess watcher = new ServerProcess(profile, modlist, instance);
            watcher.ProcessExited += OnServerProcessTerminate;
            watcher.ProcessStarted += OnServerProcessStarted;
            _serverProcesses.Add(instance, watcher);

            _lockedFolders.Add(GetCurrentServerJunction(instance));
            Task.Run(watcher.StartProcessAsync);
        }

        public void CloseServer(int instance)
        {
            if (_serverProcesses.TryGetValue(instance, out var watcher))
                watcher.Close();
        }

        public bool IsAnyServerRunning() => _serverProcesses.Count > 0;

        public bool IsClientRunning() => _clientProcess != null && _clientProcess.IsRunning;

        public bool IsFolderLocked(string folder) => _lockedFolders.Contains(folder);

        public bool IsServerRunning(int instance) => _serverProcesses.TryGetValue(instance, out var watcher) && watcher.IsRunning;

        public void KillAllServers()
        {
            foreach (ServerProcess p in _serverProcesses.Values)
                p.Kill();
        }

        public void KillClient() => _clientProcess?.Kill();

        public void KillServer(int instance)
        {
            if (_serverProcesses.TryGetValue(instance, out var watcher))
                watcher.Kill();
        }

        public void StopAllServers()
        {
            foreach (ServerProcess p in _serverProcesses.Values)
                p.Close();
        }

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

        private void AddDispatch(Action callback)
        {
            DispatcherRequest?.Invoke(this, callback);
        }

        private void FindExistingClient()
        {
            if (_clientProcess != null) return;

            var data = Tools.GetProcessesWithName(Config.FileClientBin).FirstOrDefault();
            if (data.IsEmpty) return;
            if(!TrebuchetLaunch.TryLoadPreviousLaunch(_config, out ClientProfile? profile, out ModListProfile? modlist)) return;
            if (!data.TryGetProcess(out Process? process)) return;

            _clientProcess = new ClientProcess(profile, modlist, false);
            _clientProcess.ProcessExited += OnClientProcessTerminate;
            _lockedFolders.Add(GetCurrentClientJunction());
            OnClientProcessStarted(this, _clientProcess);
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
                OnServerProcessStarted(this, watcher);                
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

        private void OnClientProcessStarted(object? sender, ClientProcess e)
        {
            if (!e.IsRunning) return;
            AddDispatch(() =>
            {
                ClientProcessStarted?.Invoke(this, new TrebuchetStartEventArgs(e.ProcessData));
            });
        }

        private void OnClientProcessTerminate(object? sender, ClientProcess e)
        {
            AddDispatch(() =>
            {
                if (_clientProcess != e) return;
                _clientProcess = null;
                ClientTerminated?.Invoke(this, EventArgs.Empty);
                _lockedFolders.Remove(GetCurrentClientJunction());
            });
        }

        private void OnServerProcessStarted(object? sender, ServerProcess e)
        {
            if (!e.IsRunning) return;

            AddDispatch(() =>
            {
                ServerProcessStarted?.Invoke(this, new TrebuchetStartEventArgs(e.ProcessData, e.ServerInstance));
            });
        }

        private void OnServerProcessTerminate(object? sender, ServerProcess e)
        {
            AddDispatch(() =>
            {
                if (!_serverProcesses.ContainsKey(e.ServerInstance)) return;
                _serverProcesses.Remove(e.ServerInstance);

                if (!e.Closed && _config.RestartWhenDown)
                {
                    var watcher = new ServerProcess(e.Profile, e.Modlist, e.ServerInstance);
                    _serverProcesses.Add(watcher.ServerInstance, watcher);
                    watcher.ProcessStarted += OnServerProcessStarted;
                    watcher.ProcessExited += OnServerProcessTerminate;
                    Task.Run(watcher.StartProcessAsync);
                }
                else
                {
                    ServerTerminated?.Invoke(this, e.ServerInstance);
                    _lockedFolders.Remove(GetCurrentServerJunction(e.ServerInstance));
                }
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