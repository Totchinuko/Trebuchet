using Goog;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GoogLib
{
    public class ServerWatcher
    {
        private bool _closed;
        private Config _config;
        private DateTime _lastResponsive;
        private ServerProfile _profile;
        private string _profileFolder;
        private int _serverInstance;

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

        public Process? Process { get; private set; }

        public ServerProfile Profile => _profile;

        public int ServerInstance { get => _serverInstance; }

        public void Close()
        {
            if (Process == null) return;

            _closed = true;
            Process.CloseMainWindow();
        }

        public void Kill()
        {
            if (Process == null) return;

            _closed = true;
            Process.Kill();
        }

        public void ProcessRefresh()
        {
            if (Process == null) return;
            Process.Refresh();

            if (Process.Responding)
                _lastResponsive = DateTime.UtcNow;
            if (IsZombie && _profile.KillZombies)
                Process.Kill();
        }

        [MemberNotNull("Process")]
        public void SetRunningProcess(Process process)
        {
            if (Process != null)
                throw new Exception("Cannot start a process already started.");

            Process = process;
            Process.EnableRaisingEvents = true;
            Process.Exited -= OnProcessExited;
            Process.Exited += OnProcessExited;
        }

        [MemberNotNull("Process")]
        public void StartProcess()
        {
            if (Process != null)
                throw new Exception("Cannot start a process already started.");
            Process = new Process();
            Process.StartInfo.FileName = Path.Combine(_config.InstallPath,
                _config.VersionFolder,
                Config.FolderServerInstances,
                string.Format(Config.FolderInstancePattern, _serverInstance),
                Config.FolderGameBinaries,
                Config.FileServerBin);

            Process.StartInfo.WorkingDirectory = Path.Combine(_config.InstallPath,
                _config.VersionFolder,
                Config.FolderServerInstances,
                string.Format(Config.FolderInstancePattern, _serverInstance),
                Config.FolderGameBinaries);

            List<string> args = new List<string>() { _profile.Map };
            if (_profile.Log) args.Add(Config.GameArgsLog);
            if (_profile.UseAllCores) args.Add(Config.GameArgsUseAllCore);
            args.Add(string.Format(Config.ServerArgsMaxPlayers, 10));
            args.Add(string.Format(Config.GameArgsModList, Path.Combine(_profileFolder, Config.FileGeneratedModlist)));

            Process.StartInfo.Arguments = string.Join(" ", args);
            
            Process.StartInfo.UseShellExecute = false;
            Process.EnableRaisingEvents = true;
            Process.Exited += OnProcessExited;
            Process.Start();
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            if (Process == null) return;
            Process.Exited -= OnProcessExited;
            Process = null;

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