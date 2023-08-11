using Goog;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GoogLib
{
    public class ServerWatcher
    {
        private Config _config;
        private DateTime _lastResponsive;
        private Process? _process;
        private ServerProfile _profile;
        private string _profileFolder;
        private int _serverInstance;
        private bool _closed;

        public ServerWatcher(Config config, ServerProfile profile, int instance)
        {
            _config = config;
            _profile = profile;
            _lastResponsive = DateTime.UtcNow;
            _serverInstance = instance;
            _profileFolder = Path.GetDirectoryName(_profile.FilePath) ?? throw new Exception("Invalid folder directory.");
        }

        public event EventHandler<ServerWatcher>? ProcessExited;

        public event EventHandler<ServerWatcher>? ProcessRestarted;

        public bool IsZombie { get => (_lastResponsive + TimeSpan.FromSeconds(_profile.ZombieCheckSeconds)) < DateTime.UtcNow; }

        public int ServerInstance { get => _serverInstance; }

        public Process? Process => _process;

        public void Close()
        {
            if (_process == null) return;

            _closed = true;
            _process.CloseMainWindow();
        }

        public void Kill()
        {
            if (_process == null) return;

            _closed = true;
            _process.Kill();
        }

        public void ProcessRefresh()
        {
            if (_process == null) return;
            _process.Refresh();

            if (_process.Responding)
                _lastResponsive = DateTime.UtcNow;
            if (IsZombie && _profile.KillZombies)
                _process.Kill();
        }

        [MemberNotNull("_process", "Process")]
        public void StartProcess()
        {
            if (_process != null)
                throw new Exception("Cannot start a process already started.");
            _process = new Process();
            _process.StartInfo.FileName = Path.Combine(_config.InstallPath,
                _config.VersionFolder,
                Config.FolderServerInstances,
                string.Format(Config.FolderInstancePattern, _serverInstance),
                Config.FolderGameBinaries,
                Config.FileServerBin);

            List<string> args = new List<string>() { _profile.Map };
            if (_profile.Log) args.Add(Config.GameArgsLog);
            if (_profile.UseAllCores) args.Add(Config.GameArgsUseAllCore);
            args.Add(string.Format(Config.ServerArgsMaxPlayers, 10));
            args.Add(string.Format(Config.GameArgsModList, Path.Combine(_profileFolder, Config.FileGeneratedModlist)));

            _process.StartInfo.Arguments = string.Join(" ", args);
            _process.StartInfo.UseShellExecute = false;
            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExited;
            _process.Start();
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            if (_process == null) return;
            _process.Exited -= OnProcessExited;
            _process = null;

            if (_profile.RestartWhenDown && !_closed)
            {
                StartProcess();
                ProcessRestarted?.Invoke(sender, this);
            }
            else
                ProcessExited?.Invoke(sender, this);
        }
    }
}