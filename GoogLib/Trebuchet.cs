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

        public void CatapultClient(string profileName, string modlistName)
        {
            if (_clientProcess != null)
                throw new Exception("Client is already running.");

            string profilePath = ClientProfile.GetPath(_config, profileName);
            if (!File.Exists(profilePath)) throw new Exception("Unknown game profile.");
            ClientProfile profile = ClientProfile.LoadProfile(_config, profilePath);

            if (_lockedFolders.Contains(profile.ProfileFolder))
                throw new Exception("Profile folder is currently locked by another process.");

            string modlistPath = ModListProfile.GetPath(_config, modlistName);
            if (!File.Exists(modlistPath)) throw new Exception("Unknown Mod List.");
            ModListProfile modlist = ModListProfile.LoadProfile(_config, modlistPath);

            modlist.ResolveModsPath(modlist.Modlist, out List<string> result, out List<string> errors);
            if (errors.Count > 0)
                throw new Exception("Some mods path could not be resolved.");

            File.WriteAllLines(Path.Combine(profile.ProfileFolder, Config.FileGeneratedModlist), result);

            SetupJunction(_config.ClientPath, profile.ProfileFolder);

            IniConfigurator configurator = new IniConfigurator(_config);
            configurator.WriteIniConfigs(profile, _config.ClientPath);
            configurator.FlushConfigs();

            _clientProcess = new ClientProcess(profile.GetBinaryPath(profile.UseBattleEye), profile.GetClientArgs());
            _clientProcess.ProcessExited += OnClientProcessTerminate;
            _clientProcess.ProcessStarted += OnClientProcessStarted;

            _lockedFolders.Add(GetCurrentClientJunction());
            Task.Run(_clientProcess.StartProcessAsync);
        }

        public void CatapultServer(string profileName, string modlistName, int instance)
        {
            if (_serverProcesses.ContainsKey(instance))
                throw new Exception("Server instance is already running.");

            string profilePath = ServerProfile.GetPath(_config, profileName);
            if (!File.Exists(profilePath)) throw new Exception("Unknown server profile.");
            ServerProfile profile = ServerProfile.LoadProfile(_config, profilePath);

            if (_lockedFolders.Contains(profile.ProfileFolder))
                throw new Exception("Profile folder is currently locked by another process.");

            string modlistPath = ModListProfile.GetPath(_config, modlistName);
            if (!File.Exists(modlistPath)) throw new Exception("Unknown Mod List.");
            ModListProfile modlist = ModListProfile.LoadProfile(_config, modlistPath);

            modlist.ResolveModsPath(modlist.Modlist, out List<string> result, out List<string> errors);
            if (errors.Count > 0)
                throw new Exception("Some mods path could not be resolved.");

            File.WriteAllLines(Path.Combine(profile.ProfileFolder, Config.FileGeneratedModlist), result);

            SetupJunction(GetInstancePath(instance), profile.ProfileFolder);

            IniConfigurator configurator = new IniConfigurator(_config);
            configurator.WriteIniConfigs(profile, GetInstancePath(instance));
            configurator.FlushConfigs();

            ServerProcess watcher = new ServerProcess(GetServerIntanceBinary(instance), profile.GetServerArgs(instance), instance);
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

        public IEnumerable<string> GetInstanceActiveWorkshopMods(int instance)
        {
            string path = Path.Combine(GetInstancePath(instance), Config.FolderGameSave, Config.FileGeneratedModlist);
            if (File.Exists(path))
                return File.ReadAllLines(path);
            else
                return Enumerable.Empty<string>();
        }

        public string GetInstancePath(int instance)
        {
            return Path.Combine(_config.InstallPath, _config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, instance));
        }

        public IEnumerable<string> GetServerActiveWorkshopMods()
        {
            return _serverProcesses.Values.Select(s => GetInstanceActiveWorkshopMods(s.ServerInstance)).SelectMany(x => x).Distinct();
        }

        public string GetServerIntanceBinary(int instance)
        {
            return Path.Combine(_config.InstallPath, _config.VersionFolder, Config.FolderServerInstances, string.Format(Config.FolderInstancePattern, instance), Config.FileServerProxyBin);
        }

        public bool IsAnyServerRunning() => _serverProcesses.Count > 0;

        public bool IsClientRunning() => _clientProcess?.Process != null;

        public bool IsFolderLocked(string folder) => _lockedFolders.Contains(folder);

        public bool IsServerRunning(int instance) => _serverProcesses.TryGetValue(instance, out var watcher) && watcher.Process != null;

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

            _clientProcess?.Process?.Refresh();
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
            if (!data.TryGetProcess(out Process? process)) return;

            _clientProcess = new ClientProcess(process);
            _clientProcess.ProcessExited += OnClientProcessTerminate;
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

                ServerProcess watcher = new ServerProcess(process, p.filename, p.args, instance);
                watcher.ProcessExited += OnServerProcessTerminate;
                _serverProcesses.Add(instance, watcher);
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
            string path = Path.Combine(GetInstancePath(instance), Config.FolderGameSave);
            if (JunctionPoint.Exists(path))
                return JunctionPoint.GetTarget(path);
            else return string.Empty;
        }

        private void OnClientProcessStarted(object? sender, ClientProcess e)
        {
            if (e.Process == null) return;
            AddDispatch(() =>
            {
                ClientProcessStarted?.Invoke(this, new TrebuchetStartEventArgs(e.Process));
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
            if (e.Process == null) return;

            AddDispatch(() =>
            {
                ServerProcessStarted?.Invoke(this, new TrebuchetStartEventArgs(e.Process, e.ServerInstance));
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
                    var watcher = new ServerProcess(e.Filename, e.Args, e.ServerInstance);
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