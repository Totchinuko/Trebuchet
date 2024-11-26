using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet
{
    public class ServerProcess : IDisposable
    {
        private readonly object _processLock = new object();
        private ProcessServerDetails _details;
        private bool _isRunning = false;
        private DateTime _lastResponse;
        private Process? _process;
        private ProcessData _processData;
        private ServerState _serverState;
        private SourceQueryReader? _sourceQueryReader;
        private ConcurrentQueue<Action> _threadQueue = new ConcurrentQueue<Action>();

        public ServerProcess(ServerProfile profile, ModListProfile modlist, int instance)
        {
            ServerInstance = instance;
            Profile = profile;
            Modlist = modlist;
            ProcessDetails = new ProcessServerDetails(instance, profile, modlist);
            Rcon = new Rcon(new IPEndPoint(IPAddress.Loopback, ProcessDetails.RconPort), ProcessDetails.RconPassword, 5 * 1000, 30 * 3000);
            Console = new MixedConsole(Rcon);
        }

        public event EventHandler<ProcessServerDetailsEventArgs>? ProcessStateChanged;

        public IConsole Console { get; }

        public ModListProfile Modlist { get; }

        public ProcessData ProcessData
        {
            get
            {
                lock (_processLock)
                    return _processData;
            }
            private set
            {
                lock (_processLock)
                    _processData = value;
            }
        }

        public ProcessServerDetails ProcessDetails
        {
            get
            {
                lock (_processLock)
                    return _details;
            }
            [MemberNotNull(nameof(_details))]
            private set
            {
                lock (_processLock)
                    _details = value;
            }
        }

        public ServerProfile Profile { get; }

        public Rcon Rcon { get; }

        public int ServerInstance { get; }

        public void Close()
        {
            Invoke(() =>
            {
                if (_process == null) return;

                OnProcessStateChanged(ProcessState.STOPPING);
                _process.CloseMainWindow();
            });
        }

        public void Dispose()
        {
            Invoke(() =>
            {
                _process?.Dispose();
                Rcon.Dispose();
                _process = null;
                _isRunning = false;
            });
        }

        public void Invoke(Action action)
        {
            _threadQueue.Enqueue(action);
        }

        public void Kill()
        {
            Invoke(() =>
            {
                if (_process == null) return;

                OnProcessStateChanged(ProcessState.STOPPING);
                _process.Kill();
            });
        }

        public void SetExistingProcess(Process process, ProcessData data)
        {
            _process = process;
            _process.EnableRaisingEvents = true;
            _process.Exited -= OnProcessExited;
            _process.Exited += OnProcessExited;
            CreateQueryPortListener();
            OnProcessStateChanged(new ProcessServerDetails(ProcessDetails, Profile.GetInstancePath(ServerInstance), data, ProcessState.RUNNING));
            new Thread(ProcessThread).Start();
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

            try
            {
                await StartProcessInternal(process);
            }
            catch (Exception e)
            {
                await Log.Write(e);
                process.Dispose();
                OnProcessStateChanged(ProcessState.FAILED);
            }
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            Invoke(() =>
            {
                if (_process == null) return;

                _process.Exited -= OnProcessExited;
                _process.Dispose();
                _process = null;
                _sourceQueryReader = null;
                Rcon.Dispose();
                _isRunning = false;

                OnProcessStateChanged(ProcessDetails.State == ProcessState.STOPPING ? ProcessState.STOPPED : ProcessState.CRASHED);
            });
        }

        protected void OnProcessStateChanged(ProcessServerDetails details)
        {
            ProcessServerDetails old = ProcessDetails;
            ProcessDetails = details;
            ProcessStateChanged?.Invoke(this, new ProcessServerDetailsEventArgs(old, details));
        }

        protected void OnProcessStateChanged(ProcessState state)
        {
            OnProcessStateChanged(new ProcessServerDetails(ProcessDetails, state));
        }

        protected virtual async Task StartProcessInternal(Process process)
        {
            File.WriteAllLines(Path.Combine(Profile.ProfileFolder, Config.FileGeneratedModlist), Modlist.GetResolvedModlist());
            Profile.WriteIniFiles(ServerInstance);
            TrebuchetLaunch.WriteConfig(Profile, Modlist, ServerInstance);

            process.Start();

            ProcessData = await TryGetChildProcessData(process);
            if (ProcessData.IsEmpty || !ProcessData.TryGetProcess(out Process? childProcess))
            {
                OnProcessExited(this, EventArgs.Empty);
                return;
            }

            _process = childProcess;
            _process.PriorityClass = GetPriority(Profile.ProcessPriority);
            _process.ProcessorAffinity = (IntPtr)Tools.Clamp2CPUThreads(Profile.CPUThreadAffinity);

            OnProcessStateChanged(new ProcessServerDetails(ProcessDetails, ProcessData, ProcessState.RUNNING));
            CreateQueryPortListener();
            new Thread(ProcessThread).Start();
        }

        private void CreateQueryPortListener()
        {
            if (_sourceQueryReader != null) return;
            _sourceQueryReader = new SourceQueryReader(new IPEndPoint(IPAddress.Loopback, ProcessDetails.QueryPort), 4 * 1000, 5 * 1000);
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

        private void ProcessThread()
        {
            if (_sourceQueryReader == null)
                throw new InvalidOperationException("Query listener is not initialized.");

            _lastResponse = DateTime.UtcNow;
            var lastRefresh = DateTime.UtcNow;
            _isRunning = true;

            while (_isRunning == true)
            {
                while (_threadQueue.TryDequeue(out Action? action))
                    action();

                if ((DateTime.UtcNow - lastRefresh).TotalMilliseconds > 1000)
                {
                    lastRefresh = DateTime.UtcNow;
                    ProcessThreadRefresh();
                    UpdateOnlineStatus();
                    ProcessThreadZombieCheck();
                }

                Thread.Sleep(100);
            }
        }

        private void ProcessThreadRefresh()
        {
            if (_process == null)
                return;
            if (_sourceQueryReader == null)
                return;

            _process.Refresh();
            _sourceQueryReader.Refresh();
            _serverState = new ServerState(_sourceQueryReader.Online, _sourceQueryReader.Name, _sourceQueryReader.Players, _sourceQueryReader.MaxPlayers);
        }

        private void ProcessThreadZombieCheck()
        {
            if (_process == null)
                return;

            if (_process.Responding)
                _lastResponse = DateTime.UtcNow;

            if ((_lastResponse + TimeSpan.FromSeconds(Profile.ZombieCheckSeconds)) < DateTime.UtcNow && Profile.KillZombies)
            {
                Log.Write($"Killing zombie instance {ServerInstance} ({_process.Id})", LogSeverity.Warning);
                _process.Kill();
            }
        }

        private async Task<ProcessData> TryGetChildProcessData(Process process)
        {
            ProcessData child = ProcessData.Empty;
            while (child.IsEmpty && !process.HasExited)
            {
                child = Tools.GetFirstChildProcesses(process.Id);
                await Task.Delay(50);
            }

            return child;
        }

        private void UpdateOnlineStatus()
        {
            if (ProcessDetails.State == ProcessState.STOPPING || _process == null) return;

            if ((ProcessDetails.State != ProcessState.ONLINE && _serverState.Online) || ProcessDetails.Players != _serverState.Players)
                OnProcessStateChanged(new ProcessServerDetails(ProcessDetails, _serverState.Players, _serverState.MaxPlayers, ProcessState.ONLINE));
        }
    }
}