using Goog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GoogLib
{
    public sealed class Trebuchet
    {
        private static Regex _instanceRegex = new Regex(@"-TotInstance=([0-9+])");
        private ClientWatcher? _clientProcess;
        private Config _config;
        private Dictionary<int, ServerWatcher> _serverProcesses = new Dictionary<int, ServerWatcher>();

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
            ClientProfile profile = ClientProfile.LoadFile(profilePath);

            string modlistPath = ModListProfile.GetPath(_config, modlistName);
            if (!File.Exists(modlistPath)) throw new Exception("Unknown Mod List.");
            ModListProfile modlist = ModListProfile.LoadFile(modlistPath);

            string profileFolder = Path.GetDirectoryName(profile.FilePath) ?? throw new Exception();

            _config.ResolveModsPath(modlist.Modlist, out List<string> result, out List<string> errors);
            if (errors.Count > 0)
                throw new Exception("Some mods path could not be resolved.");

            File.WriteAllLines(Path.Combine(profileFolder, Config.FileGeneratedModlist), result);

            SetupJunction(_config.ClientPath, profileFolder);

            IniConfigurator configurator = new IniConfigurator(_config);
            configurator.WriteIniConfigs(profile, _config.ClientPath);
            configurator.FlushConfigs();

            _clientProcess = new ClientWatcher(_config, profile);
            _clientProcess.ProcessExited += OnClientProcessTerminate;
            _clientProcess.ProcessStarted += OnClientProcessStarted;

            Task.Run(_clientProcess.StartProcessAsync);
        }

        public void CatapultServer(string profileName, string modlistName, int instance)
        {
            if (_serverProcesses.ContainsKey(instance))
                throw new Exception("Server instance is already running.");

            string profilePath = ServerProfile.GetPath(_config, profileName);
            if (!File.Exists(profilePath)) throw new Exception("Unknown server profile.");
            ServerProfile profile = ServerProfile.LoadFile(profilePath);

            string modlistPath = ModListProfile.GetPath(_config, modlistName);
            if (!File.Exists(modlistPath)) throw new Exception("Unknown Mod List.");
            ModListProfile modlist = ModListProfile.LoadFile(modlistPath);

            string profileFolder = Path.GetDirectoryName(profile.FilePath) ?? throw new Exception();

            _config.ResolveModsPath(modlist.Modlist, out List<string> result, out List<string> errors);
            if (errors.Count > 0)
                throw new Exception("Some mods path could not be resolved.");

            File.WriteAllLines(Path.Combine(profileFolder, Config.FileGeneratedModlist), result);

            SetupJunction(_config.GetInstancePath(instance), profileFolder);

            IniConfigurator configurator = new IniConfigurator(_config);
            configurator.WriteIniConfigs(profile, _config.GetInstancePath(instance));
            configurator.FlushConfigs();

            ServerWatcher watcher = new ServerWatcher(_config, profile, instance);
            watcher.ProcessExited += OnServerProcessTerminate;
            watcher.ProcessStarted += OnServerProcessStarted;
            _serverProcesses.Add(instance, watcher);

            Task.Run(watcher.StartProcessAsync);
        }

        public void CloseServer(int instance)
        {
            if (_serverProcesses.TryGetValue(instance, out var watcher))
                watcher.Close();
        }

        public bool IsClientRunning() => _clientProcess?.Process != null;

        public bool IsServerRunning(int instance) => _serverProcesses.TryGetValue(instance, out var watcher) && watcher.Process != null;

        public void KillAllServers()
        {
            foreach (ServerWatcher p in _serverProcesses.Values)
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
            foreach (ServerWatcher p in _serverProcesses.Values)
                p.Close();
        }

        public void TickTrebuchet()
        {
            FindExistingClient();
            FindExistingServers();

            foreach (ServerWatcher servers in _serverProcesses.Values)
                servers.ProcessRefresh();
            _clientProcess?.Process?.Refresh();
        }

        private void AddDispatch(Action callback)
        {
            DispatcherRequest?.Invoke(this, callback);
        }

        private void FindExistingClient()
        {
            if (_clientProcess != null) return;

            List<ProcessData> processes = Tools.GetProcessesWithName(Config.FileClientBin);
            if (processes.Count == 0) return;
            if (!processes[0].TryGetProcess(out Process? process)) return;

            _clientProcess = new ClientWatcher(process);
            _clientProcess.ProcessExited += OnClientProcessTerminate;
            OnClientProcessStarted(this, _clientProcess);
        }

        private void FindExistingServers()
        {
            List<ProcessData> processes = Tools.GetProcessesWithName(Config.FileServerBin);

            foreach (var p in processes)
            {
                if (!GetInstance(p.args, out int instance)) continue;
                if (_serverProcesses.ContainsKey(instance)) continue;
                if (!p.TryGetProcess(out Process? process)) continue;

                ServerWatcher watcher = new ServerWatcher(_config, process, p.filename, p.args, instance);
                watcher.ProcessExited += OnServerProcessTerminate;
                _serverProcesses.Add(instance, watcher);
                OnServerProcessStarted(this, watcher);
            }
        }

        private void OnClientProcessStarted(object? sender, ClientWatcher e)
        {
            if (e.Process == null) return;
            AddDispatch(() =>
            {
                ClientProcessStarted?.Invoke(this, new TrebuchetStartEventArgs(e.Process));
            });
        }

        private void OnClientProcessTerminate(object? sender, ClientWatcher e)
        {
            AddDispatch(() =>
            {
                if (_clientProcess != e) return;
                _clientProcess = null;
                ClientTerminated?.Invoke(this, EventArgs.Empty);
            });
        }

        private void OnServerProcessStarted(object? sender, ServerWatcher e)
        {
            if (e.Process == null) return;

            AddDispatch(() =>
            {
                ServerProcessStarted?.Invoke(this, new TrebuchetStartEventArgs(e.Process, e.ServerInstance));
            });
        }

        private void OnServerProcessTerminate(object? sender, ServerWatcher e)
        {
            AddDispatch(() =>
            {
                if (!_serverProcesses.ContainsKey(e.ServerInstance)) return;
                _serverProcesses.Remove(e.ServerInstance);

                if (!e.Closed && _config.RestartWhenDown)
                {
                    var watcher = new ServerWatcher(_config, e.Filename, e.Args, e.ServerInstance);
                    _serverProcesses.Add(watcher.ServerInstance, watcher);
                    watcher.ProcessStarted += OnServerProcessStarted;
                    watcher.ProcessExited += OnServerProcessTerminate;
                    Task.Run(watcher.StartProcessAsync);
                }
                else
                    ServerTerminated?.Invoke(this, e.ServerInstance);
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