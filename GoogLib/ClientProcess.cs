using Goog;
using System.Diagnostics;

namespace GoogLib
{
    public class ClientProcess
    {
        private Process? _process;

        public ClientProcess(ClientProfile profile, ModListProfile modlist, bool isBattleEye)
        {
            Profile = profile;
            Modlist = modlist;
            IsBattleEye = isBattleEye;
        }

        public ClientProcess(ClientProfile profile, ModListProfile modlist, bool isBattleEye, Process process, ProcessData data)
        {
            Profile = profile;
            Modlist = modlist;
            IsBattleEye = isBattleEye;
            ProcessData = data;

            _process = process;
            _process.EnableRaisingEvents = true;
            _process.Exited -= OnProcessExited;
            _process.Exited += OnProcessExited;
        }

        public event EventHandler? ProcessExited;

        public event EventHandler<TrebuchetFailEventArgs>? ProcessFailed;

        public event EventHandler<TrebuchetStartEventArgs>? ProcessStarted;

        public bool IsBattleEye { get; }

        public bool IsRunning => _process != null;

        public ModListProfile Modlist { get; }

        public ProcessData ProcessData { get; private set; }

        public ClientProfile Profile { get; }

        public void Kill()
        {
            _process?.Kill();
        }

        public async Task StartProcessAsync()
        {
            if (_process != null)
                throw new Exception("Cannot start a process already started.");

            string filename = Profile.GetBinaryPath(IsBattleEye);
            string args = Profile.GetClientArgs();

            string? dir = Path.GetDirectoryName(filename);
            if (dir == null) 
                throw new Exception($"Failed to restart process, invalid directory {filename}");

            Process process = new Process();
            process.StartInfo.FileName = filename;

            process.StartInfo.WorkingDirectory = dir;

            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;

            try
            {
                await StartProcessInternal(process);
            }
            catch (Exception ex)
            {
                process.Dispose();
                OnProcessFailed(ex);
            }
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            if (_process == null) return;
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
            ProcessStarted?.Invoke(this, new TrebuchetStartEventArgs(data));
        }

        //If we start with battle eye, launched process is not going to be the actual game.
        protected virtual async Task StartProcessInternal(Process process)
        {
            File.WriteAllLines(Path.Combine(Profile.ProfileFolder, Config.FileGeneratedModlist), Modlist.GetResolvedModlist());
            Profile.WriteIniFiles();
            TrebuchetLaunch.WriteConfig(Profile, Modlist);

            process.Start();

            ProcessData target = ProcessData.Empty;
            while (target.IsEmpty && !process.HasExited)
            {
                target = Tools.GetProcessesWithName(Config.FileClientBin).FirstOrDefault();
                await Task.Delay(50);
            }

            if (target.IsEmpty) return;
            if (!target.TryGetProcess(out Process? targetProcess)) return;

            _process = targetProcess;
            ProcessData = target;

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