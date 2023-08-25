using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using TrebuchetLib;
using Yuu.Ini;
using static SteamKit2.Internal.PublishedFileDetails;

namespace Trebuchet
{
    public class ServerProcess : IDisposable
    {
        private readonly object _processLock = new object();
        private ProcessServerDetails _details;
        private Process? _process;
        private ServerState _serverState;
        private SourceQueryReader? _sourceQueryReader;

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

        public ProcessData ProcessData { get; private set; }

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

        public IRcon Rcon { get; }

        public int ServerInstance { get; }

        public void Close()
        {
            lock (_processLock)
            {
                if (_process == null) return;

                OnProcessStateChanged(ProcessState.STOPPING);
                _process.CloseMainWindow();
            }
        }

        public void Dispose()
        {
            lock (_processLock)
            {
                _process?.Dispose();
                _process = null;
            }
        }

        public void Kill()
        {
            lock (_processLock)
            {
                if (_process == null) return;

                OnProcessStateChanged(ProcessState.STOPPING);
                _process.Kill();
            }
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
                Log.Write(e);
                process.Dispose();
                OnProcessStateChanged(ProcessState.FAILED);
            }
        }

        protected virtual void OnProcessExited(object? sender, EventArgs e)
        {
            lock (_processLock)
            {
                if (_process == null) return;

                _process.Exited -= OnProcessExited;
                _process.Dispose();
                _process = null;
                _sourceQueryReader = null;
            }
            OnProcessStateChanged(ProcessDetails.State == ProcessState.STOPPING ? ProcessState.STOPPED : ProcessState.CRASHED);
        }

        protected void OnProcessStateChanged(ProcessServerDetails details)
        {
            ProcessStateChanged?.Invoke(this, new ProcessServerDetailsEventArgs(ProcessDetails, details));
            ProcessDetails = details;
        }

        protected void OnProcessStateChanged(ProcessState state)
        {
            OnProcessStateChanged(new ProcessServerDetails(_details, state));
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

            int timeSpan = Profile.ZombieCheckSeconds;
            bool killZombies = Profile.KillZombies;
            DateTime last = DateTime.UtcNow;
            int instance = ServerInstance;

            while (true)
            {
                lock (_processLock)
                    if (_process == null) return;

                ProcessThreadRefresh();

                UpdateOnlineStatus();

                ProcessThreadZombieCheck(timeSpan, killZombies, last, instance);

                Thread.Sleep(1000);
            }
        }

        private void ProcessThreadRefresh()
        {
            lock (_processLock)
            {
                if (_process == null)
                    throw new InvalidOperationException("Process is not initialized.");
                if (_sourceQueryReader == null)
                    throw new InvalidOperationException("Query listener is not initialized.");

                _process.Refresh();
                _sourceQueryReader.Refresh();
                _serverState = new ServerState(_sourceQueryReader.Online, _sourceQueryReader.Name, _sourceQueryReader.Players, _sourceQueryReader.MaxPlayers);
            }
        }

        private void ProcessThreadZombieCheck(int timeSpan, bool killZombies, DateTime last, int instance)
        {
            lock (_processLock)
            {
                if (_process == null)
                    throw new InvalidOperationException("Process is not initialized.");

                if (_process.Responding)
                    last = DateTime.UtcNow;

                if ((last + TimeSpan.FromSeconds(timeSpan)) < DateTime.UtcNow && killZombies)
                {
                    Log.Write($"Killing zombie instance {instance} ({_process.Id})", LogSeverity.Warning);
                    _process.Kill();
                }
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
            ServerState state;
            lock (_processLock)
                state = _serverState;

            if ((ProcessDetails.State != ProcessState.ONLINE && state.Online) || ProcessDetails.Players != state.Players)
                OnProcessStateChanged(new ProcessServerDetails(ProcessDetails, state.Players, state.MaxPlayers, ProcessState.ONLINE));
        }
    }
}