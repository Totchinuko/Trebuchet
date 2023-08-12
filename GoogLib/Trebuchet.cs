using Goog;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace GoogLib
{
    public sealed class Trebuchet
    {
        private ClientWatcher? _clientProcesses;
        private Config _config;
        private Dictionary<int, ServerWatcher> _serverProcesses = new Dictionary<int, ServerWatcher>();

        public Trebuchet(Config config)
        {
            _config = config;

            FindExistingClient();
            FindExistingServers();
        }

        public event EventHandler? ClientTerminated;

        public event EventHandler<int>? ServerRestarted;

        public event EventHandler<int>? ServerTerminated;

        public event EventHandler<Action> DispatcherRequest;

        public ClientWatcher? ClientProcess => _clientProcesses;

        public ReadOnlyDictionary<int, ServerWatcher> ServerProcesses => _serverProcesses.AsReadOnly();

        public Process CatapultClient(string profileName, string modlistName)
        {
            if (_clientProcesses != null)
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

            _clientProcesses = new ClientWatcher(_config, profile);
            _clientProcesses.ProcessExited += OnClientProcessTerminate;
            _clientProcesses.StartProcess();

            _config.ClientPastLaunch = new PastLaunch(_clientProcesses.Process.Id, profileName, modlistName);
            _config.SaveFile();

            return _clientProcesses.Process;
        }

        public Process CatapultServer(string profileName, string modlistName, int instance)
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
            watcher.ProcessRestarted += OnServerProcessRestarted;
            watcher.StartProcess();
            _serverProcesses.Add(instance, watcher);

            _config.SetServerPastLaunch(new PastLaunch(watcher.Process.Id, profileName, modlistName), instance);
            _config.SaveFile();

            return watcher.Process;
        }

        public void KillAllServers()
        {
            foreach (ServerWatcher p in _serverProcesses.Values)
                p.Kill();
        }

        public void StopAllServers()
        {
            foreach (ServerWatcher p in _serverProcesses.Values)
                p.Close();
        }

        public void TickTrebuchet()
        {
            foreach (ServerWatcher servers in _serverProcesses.Values)
                servers.ProcessRefresh();
            _clientProcesses?.Process?.Refresh();
        }

        private void AddDispatch(Action callback)
        {
            DispatcherRequest?.Invoke(this, callback);
        }

        private void FindExistingClient()
        {
            if (_config.ClientPastLaunch == null) return;

            Process process;
            try
            {
                process = Process.GetProcessById(_config.ClientPastLaunch.Pid);
            }
            catch
            {
                _config.ClientPastLaunch = null;
                return;
            }

            string profilePath = ClientProfile.GetPath(_config, _config.ClientPastLaunch.Profile);
            if (!File.Exists(profilePath)) throw new Exception("Unknown game profile.");
            ClientProfile profile = ClientProfile.LoadFile(profilePath);

            _clientProcesses = new ClientWatcher(_config, profile);
            _clientProcesses.SetRunningProcess(process);
            _clientProcesses.ProcessExited += OnClientProcessTerminate;
        }

        private void FindExistingServer(int instance)
        {
            if (!_config.TryGetServerPastLaunch(instance, out PastLaunch? pastLaunch)) return;

            Process process;
            try
            {
                process = Process.GetProcessById(pastLaunch.Pid);
            }
            catch
            {
                _config.ClientPastLaunch = null;
                return;
            }

            string profilePath = ClientProfile.GetPath(_config, pastLaunch.Profile);
            if (!File.Exists(profilePath)) throw new Exception("Unknown game profile.");
            ServerProfile profile = ServerProfile.LoadFile(profilePath);

            ServerWatcher watcher = new ServerWatcher(_config, profile, instance);
            watcher.SetRunningProcess(process);
            watcher.ProcessExited += OnServerProcessTerminate;
            watcher.ProcessRestarted += OnServerProcessRestarted;
            _serverProcesses.Add(instance, watcher);
        }

        private void FindExistingServers()
        {
            for (int i = 0; i < _config.ServerInstanceCount; i++)
                FindExistingServer(i);
        }

        private void OnClientProcessTerminate(object? sender, ClientWatcher e)
        {
            AddDispatch(() =>
            {
                if (_clientProcesses != e) return;
                _clientProcesses = null;
                _config.ClientPastLaunch = null;
                _config.SaveFile();
                ClientTerminated?.Invoke(this, EventArgs.Empty);
            });
        }

        private void OnServerProcessRestarted(object? sender, ServerWatcher e)
        {
            AddDispatch(() =>
            {
                _config.SetServerPastLaunch(new PastLaunch(e.Process?.Id ?? -1, e.Profile.ProfileName, string.Empty), e.ServerInstance);
                _config.SaveFile();

                ServerRestarted?.Invoke(this, e.ServerInstance);
            });            
        }

        private void OnServerProcessTerminate(object? sender, ServerWatcher e)
        {
            AddDispatch(() =>
            {
                if (!_serverProcesses.ContainsKey(e.ServerInstance)) return;
                _serverProcesses.Remove(e.ServerInstance);

                _config.SetServerPastLaunch(null, e.ServerInstance);
                _config.SaveFile();

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