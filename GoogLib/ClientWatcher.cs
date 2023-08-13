    using Goog;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GoogLib
{
    public class ClientWatcher
    {
        private string _filename = string.Empty;
        private string _args = string.Empty;

        public ClientWatcher(Config config, ClientProfile profile)
        {
            string profileFolder = Path.GetDirectoryName(profile.FilePath) ?? throw new Exception("Invalid folder directory.");

            _filename = Path.Combine(config.ClientPath,
                Config.FolderGameBinaries,
                (profile.UseBattleEye ? Config.FileClientBEBin : Config.FileClientBin));

            List<string> args = new List<string>();
            if (profile.Log) args.Add(Config.GameArgsLog);
            if (profile.UseAllCores) args.Add(Config.GameArgsUseAllCore);
            args.Add(string.Format(Config.GameArgsModList, Path.Combine(profileFolder, Config.FileGeneratedModlist)));

            _args = string.Join(" ", args);
        }

        public ClientWatcher(Process process)
        {
            Process = process;
            Process.EnableRaisingEvents = true;
            Process.Exited -= OnProcessExited;
            Process.Exited += OnProcessExited;
        }

        public event EventHandler<ClientWatcher>? ProcessExited;
        public event EventHandler<ClientWatcher>? ProcessStarted;
        public Process? Process { get; private set; }

        public void Close()
        {
            Process?.CloseMainWindow();
        }

        public void Kill()
        {
            Process?.Kill();
        }

        public async Task StartProcessAsync()
        {
            if (Process != null)
                throw new Exception("Cannot start a process already started.");

            string? dir = Path.GetDirectoryName(_filename);
            if (dir == null) throw new Exception($"Failed to restart process, invalid directory {_filename}");

            Process process = new Process();
            process.StartInfo.FileName = _filename;

            process.StartInfo.WorkingDirectory = dir;

            process.StartInfo.Arguments = _args;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;
            Process = process;
            await Task.Run(() => {
                Process.Start();
                OnProcessStarted();
            });
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            if (Process == null) return;
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