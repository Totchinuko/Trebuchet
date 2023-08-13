using Goog;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GoogLib
{
    public class ServerWatcher
    {
        private bool _closed;
        private DateTime _lastResponsive;

        private string _filename = string.Empty;
        private string _args = string.Empty;

        private bool _zombieCheck;
        private int _zombieCheckDuration;

        private int _serverInstance;

        public string Filename => _filename;
        public string Args => _args;

        public bool Closed => _closed;

        public ServerWatcher(Config config, ServerProfile profile, int instance)
        {
            _lastResponsive = DateTime.UtcNow;
            _zombieCheck = config.KillZombies;
            _zombieCheckDuration = config.ZombieCheckSeconds;
            _serverInstance = instance;

            _filename = Path.Combine(config.InstallPath,
                config.VersionFolder,
                Config.FolderServerInstances,
                string.Format(Config.FolderInstancePattern, instance),
                Config.FileServerProxyBin);

            string? profileFolder = Path.GetDirectoryName(profile.FilePath) ?? throw new Exception("Invalid folder directory.");

            List<string> args = new List<string>() { profile.Map };
            if (profile.Log) args.Add(Config.GameArgsLog);
            if (profile.UseAllCores) args.Add(Config.GameArgsUseAllCore);
            args.Add(string.Format(Config.ServerArgsMaxPlayers, 10));
            args.Add(string.Format(Config.GameArgsModList, Path.Combine(profileFolder, Config.FileGeneratedModlist)));
            args.Add($"-TotInstance={instance}");

            _args = string.Join(" ", args);
        }

        public ServerWatcher(Config config, Process process, string filename, string args, int instance)
        {
            _lastResponsive = DateTime.UtcNow;
            _zombieCheck = config.KillZombies;
            _zombieCheckDuration = config.ZombieCheckSeconds;
            _serverInstance = instance;

            _filename = filename;
            _args = args;

            Process = process;
            Process.EnableRaisingEvents = true;
            Process.Exited -= OnProcessExited;
            Process.Exited += OnProcessExited;
        }

        public ServerWatcher(Config config, string filename, string args, int instance)
        {
            _lastResponsive = DateTime.UtcNow;
            _zombieCheck = config.KillZombies;
            _zombieCheckDuration = config.ZombieCheckSeconds;
            _serverInstance = instance;

            _args = args;
            _filename = filename;
        }

        public event EventHandler<ServerWatcher>? ProcessExited;

        public event EventHandler<ServerWatcher>? ProcessStarted;

        public bool IsZombie { get => (_lastResponsive + TimeSpan.FromSeconds(_zombieCheckDuration)) < DateTime.UtcNow; }

        public Process? Process { get; private set; }

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
            if (IsZombie && _zombieCheck)
                Process.Kill();
        }

        public async Task StartProcessAsync()
        {
            Process process = new Process();

            string? dir = Path.GetDirectoryName(_filename);
            if (dir == null) throw new Exception($"Failed to restart process, invalid directory {_filename}");

            process.StartInfo.FileName = _filename;
            process.StartInfo.WorkingDirectory = dir;
            process.StartInfo.Arguments = _args;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;

            await StartProcessInternal(process);
        }

        // Why do we do this ? To avoid app freeze we start the server with the root server boot exe. That create the actual server process in a child process so we need to retreve it.
        // Its usually done in an instant, but since I'm not sure that this could be prone to race condition, I'm waiting for it just in case.
        protected virtual async Task StartProcessInternal(Process process)
        {
            process.Start();

            ProcessData child = ProcessData.Empty;
            while (child.IsEmpty && !process.HasExited)
            {
                child = Tools.GetFirstChildProcesses(process.Id);
                await Task.Delay(50);
            }

            if (child.IsEmpty) return;
            if (!child.TryGetProcess(out Process? childProcess)) return;

            Process = childProcess;
            OnProcessStarted();
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            if (Process == null) return;

            Process.Exited -= OnProcessExited;
            Process.Dispose();
            Process = null;
            ProcessExited?.Invoke(sender, this);
        }

        protected virtual void OnProcessStarted() 
        {
            ProcessStarted?.Invoke(this, this);
        }
    }
}