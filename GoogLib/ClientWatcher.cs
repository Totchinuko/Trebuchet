    using Goog;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GoogLib
{
    public class ClientWatcher
    {
        private Config _config;
        private ClientProfile _profile;
        private string _profileFolder;

        public ClientWatcher(Config config, ClientProfile profile)
        {
            _config = config;
            _profile = profile;
            _profileFolder = Path.GetDirectoryName(_profile.FilePath) ?? throw new Exception("Invalid folder directory.");
        }

        public event EventHandler<ClientWatcher>? ProcessExited;
        public Process? Process { get; private set; }

        public void Close()
        {
            Process?.CloseMainWindow();
        }

        public void Kill()
        {
            Process?.Kill();
        }

        [MemberNotNull("Process")]
        public void StartProcess()
        {
            if (Process != null)
                throw new Exception("Cannot start a process already started.");

            Process = new Process();
            Process.StartInfo.FileName = Path.Combine(_config.ClientPath,
                Config.FolderGameBinaries,
                (_profile.UseBattleEye ? Config.FileClientBEBin : Config.FileClientBin));

            Process.StartInfo.WorkingDirectory = Path.Combine(_config.ClientPath,
                Config.FolderGameBinaries);

            List<string> args = new List<string>();
            if (_profile.Log) args.Add(Config.GameArgsLog);
            if (_profile.UseAllCores) args.Add(Config.GameArgsUseAllCore);
            args.Add(string.Format(Config.GameArgsModList, Path.Combine(_profileFolder, Config.FileGeneratedModlist)));

            Process.StartInfo.Arguments = string.Join(" ", args);
            Process.StartInfo.UseShellExecute = false;
            Process.EnableRaisingEvents = true;
            Process.Exited += OnProcessExited;
            Process.Start();
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

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            ProcessExited?.Invoke(sender, this);
        }
    }
}