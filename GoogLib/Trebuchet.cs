using Goog;
using System.Collections.ObjectModel;

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
        }

        public event EventHandler? ClientTerminated;

        public event EventHandler<int>? ServerRestarted;

        public event EventHandler<int>? ServerTerminated;

        public ClientWatcher? ClientProcesses => _clientProcesses;

        public ReadOnlyDictionary<int, ServerWatcher> ServerProcesses => _serverProcesses.AsReadOnly();

        public bool CatapultClient(ClientProfile profile)
        {
            if (_clientProcesses != null) return false;

            string profileFolder = Path.GetDirectoryName(profile.FilePath) ?? throw new Exception();
            SetupJunction(_config.ClientPath, profileFolder);

            IniConfigurator configurator = new IniConfigurator(_config);
            configurator.WriteIniConfigs(profile, _config.ClientPath);
            configurator.FlushConfigs();

            _clientProcesses = new ClientWatcher(_config, profile);
            _clientProcesses.ProcessExited += OnClientProcessTerminate;
            _clientProcesses.StartProcess();
            return true;
        }

        public bool CatapultServer(ServerProfile profile, int instance)
        {
            if (_serverProcesses.ContainsKey(instance)) return false;

            string profileFolder = Path.GetDirectoryName(profile.FilePath) ?? throw new Exception();
            SetupJunction(_config.GetInstancePath(instance), profileFolder);

            IniConfigurator configurator = new IniConfigurator(_config);
            configurator.WriteIniConfigs(profile, _config.GetInstancePath(instance));
            configurator.FlushConfigs();

            ServerWatcher watcher = new ServerWatcher(_config, profile, instance);
            watcher.ProcessExited += OnServerProcessTerminate;
            watcher.ProcessRestarted += OnServerProcessRestarted;
            watcher.StartProcess();
            _serverProcesses.Add(instance, watcher);
            return true;
        }

        public void KillAllServers()
        {
            {
                foreach (ServerWatcher p in _serverProcesses.Values)
                    p.Kill();
            }
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

        private void OnClientProcessTerminate(object? sender, ClientWatcher e)
        {
            if (_clientProcesses != e) return;
            _clientProcesses = null;
            ClientTerminated?.Invoke(this, EventArgs.Empty);
        }

        private void OnServerProcessRestarted(object? sender, ServerWatcher e)
        {
            ServerRestarted?.Invoke(this, e.ServerInstance);
        }

        private void OnServerProcessTerminate(object? sender, ServerWatcher e)
        {
            if (!_serverProcesses.ContainsKey(e.ServerInstance)) return;
            _serverProcesses.Remove(e.ServerInstance);
            ServerTerminated?.Invoke(this, e.ServerInstance);
        }

        private void SetupJunction(string gamePath, string targetPath)
        {
            string junction = Path.Combine(gamePath, Config.FolderGameSave);
            Tools.RemoveSymboliclink(junction);
            Tools.SetupSymboliclink(junction, targetPath);
        }
    }
}