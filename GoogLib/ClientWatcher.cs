using Goog;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GoogLib
{
    public class ClientWatcher
    {
        private Config _config;
        private Process? _process;
        private ClientProfile _profile;
        private string _profileFolder;

        public ClientWatcher(Config config, ClientProfile profile)
        {
            _config = config;
            _profile = profile;
            _process = new Process();
            _profileFolder = Path.GetDirectoryName(_profile.FilePath) ?? throw new Exception("Invalid folder directory.");
        }

        public event EventHandler<ClientWatcher>? ProcessExited;
        public Process? Process => _process;

        public void Close()
        {
            _process?.CloseMainWindow();
        }

        public void Kill()
        {
            _process?.Kill();
        }

        [MemberNotNull("_process", "Process")]
        public void StartProcess()
        {
            if (_process != null)
                throw new Exception("Cannot start a process already started.");

            _process = new Process();
            _process.StartInfo.FileName = Path.Combine(_config.ClientPath,
                Config.FolderGameBinaries,
                (_profile.UseBattleEye ? Config.FileClientBEBin : Config.FileClientBin));

            List<string> args = new List<string>();
            if (_profile.Log) args.Add(Config.GameArgsLog);
            if (_profile.UseAllCores) args.Add(Config.GameArgsUseAllCore);
            args.Add(string.Format(Config.GameArgsModList, Path.Combine(_profileFolder, Config.FileGeneratedModlist)));

            _process.StartInfo.Arguments = string.Join(" ", args);
            _process.StartInfo.UseShellExecute = false;
            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExited;
            _process.Start();
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            ProcessExited?.Invoke(sender, this);
        }
    }
}