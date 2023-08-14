using System.Diagnostics;

namespace GoogLib
{
    public class ClientProcess
    {
        private string _args = string.Empty;
        private string _filename = string.Empty;

        public ClientProcess(string filename, string args)
        {
            _filename = filename;
            _args = args;
        }

        public ClientProcess(Process process)
        {
            Process = process;
            Process.EnableRaisingEvents = true;
            Process.Exited -= OnProcessExited;
            Process.Exited += OnProcessExited;
        }

        public event EventHandler<ClientProcess>? ProcessExited;

        public event EventHandler<ClientProcess>? ProcessStarted;

        public Process? Process { get; private set; }

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
            await Task.Run(() =>
            {
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