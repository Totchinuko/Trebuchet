using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet
{
    public class ClientProcess
    {
        private readonly object _processLock = new object();
        private Process? _process;
        private ProcessDetails _processDetails;

        public ClientProcess(ClientProfile profile, ModListProfile modlist, bool isBattleEye)
        {
            Profile = profile;
            Modlist = modlist;
            IsBattleEye = isBattleEye;
            ProcessDetails = new ProcessDetails(profile.ProfileName, modlist.ProfileName);
        }

        public event EventHandler<ProcessDetailsEventArgs>? ProcessStateChanged;

        public bool IsBattleEye { get; }

        public ModListProfile Modlist { get; }

        public ProcessData ProcessData { get; private set; }

        public ProcessDetails ProcessDetails
        {
            get
            {
                lock (_processLock)
                    return _processDetails;
            }
            [MemberNotNull(nameof(_processDetails))]
            set
            {
                lock (_processLock)
                    _processDetails = value;
            }
        }

        public ClientProfile Profile { get; }

        public void Kill()
        {
            OnProcessStateChanged(ProcessState.STOPPING);
            _process?.Kill();
        }

        public void SetExistingProcess(Process process, ProcessData data)
        {
            _process = process;
            _process.EnableRaisingEvents = true;
            _process.Exited -= OnProcessExited;
            _process.Exited += OnProcessExited;
            OnProcessStateChanged(new ProcessDetails(ProcessDetails, data, ProcessState.RUNNING));
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
                await Log.Write(ex);
                process.Dispose();
                OnProcessStateChanged(ProcessState.FAILED);
            }
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            if (_process == null) return;
            _process.Exited -= OnProcessExited;
            _process.Dispose();
            _process = null;
            OnProcessStateChanged(ProcessDetails.State == ProcessState.STOPPING ? ProcessState.STOPPED : ProcessState.CRASHED);
        }

        protected void OnProcessStateChanged(ProcessDetails details)
        {
            var old = ProcessDetails;
            ProcessDetails = details;
            ProcessStateChanged?.Invoke(this, new ProcessDetailsEventArgs(old, details));
        }

        protected void OnProcessStateChanged(ProcessState state)
        {
            OnProcessStateChanged(new ProcessDetails(ProcessDetails, state));
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

            _process.PriorityClass = GetPriority(Profile.ProcessPriority);
            _process.ProcessorAffinity = (IntPtr)Tools.Clamp2CPUThreads(Profile.CPUThreadAffinity);

            OnProcessStateChanged(new ProcessDetails(ProcessDetails, ProcessData, ProcessState.RUNNING));
        }

        private ProcessPriorityClass GetPriority(int index)
        {
            switch (index)
            {
                case 1:
                    return ProcessPriorityClass.AboveNormal;

                case 2:
                    return ProcessPriorityClass.High;

                case 3:
                    return ProcessPriorityClass.RealTime;

                default:
                    return ProcessPriorityClass.Normal;
            }
        }
    }
}