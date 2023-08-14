using Goog;
using System.Diagnostics;

namespace GoogLib
{
    public class ServerProcess
    {
        private string _args = string.Empty;
        private bool _closed;
        private string _filename = string.Empty;
        private DateTime _lastResponsive;
        private int _serverInstance;

        public ServerProcess(Process process, string filename, string args, int instance)
        {
            _lastResponsive = DateTime.UtcNow;
            _serverInstance = instance;

            _filename = filename;
            _args = args;

            Process = process;
            Process.EnableRaisingEvents = true;
            Process.Exited -= OnProcessExited;
            Process.Exited += OnProcessExited;
        }

        public ServerProcess(string filename, string args, int instance)
        {
            _lastResponsive = DateTime.UtcNow;
            _serverInstance = instance;

            _args = args;
            _filename = filename;
        }

        public event EventHandler<ServerProcess>? ProcessExited;

        public event EventHandler<ServerProcess>? ProcessStarted;

        public string Args => _args;

        public bool Closed => _closed;

        public string Filename => _filename;

        public DateTime LastResponsive => _lastResponsive;

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
    }
}