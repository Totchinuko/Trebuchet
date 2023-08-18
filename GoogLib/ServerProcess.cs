using Goog;
using System.Diagnostics;

namespace GoogLib
{
    public class ServerProcess
    {
        private Process? _process;

        public ServerProcess(ServerProfile profile, ModListProfile modlist, int instance)
        {
            ServerInstance = instance;
            Profile = profile;
            Modlist = modlist;
        }

        public ServerProcess(ServerProfile profile, ModListProfile modlist, int instance, Process process, ProcessData data)
        {
            ServerInstance = instance;
            Profile = profile;
            Modlist = modlist;
            ProcessData = data;

            _process = process;
            _process.EnableRaisingEvents = true;
            _process.Exited -= OnProcessExited;
            _process.Exited += OnProcessExited;
        }

        public event EventHandler? ProcessExited;

        public event EventHandler<TrebuchetFailEventArgs>? ProcessFailed;

        public event EventHandler<TrebuchetStartEventArgs>? ProcessStarted;

        public bool Closed { get; private set; }

        public bool IsRunning => _process != null;

        public DateTime LastResponsive { get; private set; }

        public ModListProfile Modlist { get; }

        public ProcessData ProcessData { get; private set; }

        public ServerProfile Profile { get; }

        public int ServerInstance { get; }

        public void Close()
        {
            if (_process == null) return;

            Closed = true;
            _process.CloseMainWindow();
        }

        public void Kill()
        {
            if (_process == null) return;

            Closed = true;
            _process.Kill();
        }

        public void ProcessRefresh()
        {
            if (_process == null) return;
            _process.Refresh();

            if (_process.Responding)
                LastResponsive = DateTime.UtcNow;
        }

        public async Task StartProcessAsync()
        {
            Process process = new Process();

            string filename = Profile.GetIntanceBinary(ServerInstance);
            string args = Profile.GetServerArgs(ServerInstance);

            string? dir = Path.GetDirectoryName(filename);
            if (dir == null) 
                throw new Exception($"Failed to restart process, invalid directory {filename}");

            process.StartInfo.FileName = filename;
            process.StartInfo.WorkingDirectory = dir;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;

            await StartProcessInternal(process);
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            if (_process == null) return;

            _process.Exited -= OnProcessExited;
            _process.Dispose();
            _process = null;
            ProcessExited?.Invoke(sender, EventArgs.Empty);
        }

        protected virtual void OnProcessFailed(Exception exception)
        {
            ProcessFailed?.Invoke(this, new TrebuchetFailEventArgs(exception));
        }

        protected virtual void OnProcessStarted(ProcessData data)
        {
            ProcessStarted?.Invoke(this, new TrebuchetStartEventArgs(data, ServerInstance));
        }

        protected virtual async Task StartProcessInternal(Process process)
        {
            File.WriteAllLines(Path.Combine(Profile.ProfileFolder, Config.FileGeneratedModlist), Modlist.GetResolvedModlist());
            Profile.WriteIniFiles(ServerInstance);
            TrebuchetLaunch.WriteConfig(Profile, Modlist, ServerInstance);

            LastResponsive = DateTime.UtcNow;
            process.Start();

            ProcessData child = ProcessData.Empty;
            while (child.IsEmpty && !process.HasExited)
            {
                child = Tools.GetFirstChildProcesses(process.Id);
                await Task.Delay(50);
            }

            if (child.IsEmpty) return;
            if (!child.TryGetProcess(out Process? childProcess)) return;

            _process = childProcess;
            ProcessData = child;

            switch (Profile.ProcessPriority)
            {
                case 1:
                    _process.PriorityClass = ProcessPriorityClass.AboveNormal;
                    break;

                case 2:
                    _process.PriorityClass = ProcessPriorityClass.High;
                    break;

                case 3:
                    _process.PriorityClass = ProcessPriorityClass.RealTime;
                    break;

                default:
                    _process.PriorityClass = ProcessPriorityClass.Normal;
                    break;
            }

            _process.ProcessorAffinity = (IntPtr)Tools.Clamp2CPUThreads(Profile.CPUThreadAffinity);
            OnProcessStarted(ProcessData);
        }
    }
}