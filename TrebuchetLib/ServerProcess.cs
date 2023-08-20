using System.Diagnostics;
using Yuu.Ini;

namespace Trebuchet
{
    public class ServerProcess
    {
        private Process? _process;

        public ServerProcess(ServerProfile profile, ModListProfile modlist, int instance)
        {
            ServerInstance = instance;
            Profile = profile;
            Modlist = modlist;
            Information = new ServerInstanceInformation(ServerInstance, profile.ServerName, profile.GameClientPort, profile.SourceQueryPort, profile.RConPort);
        }

        public ServerProcess(ServerProfile profile, ModListProfile modlist, int instance, Process process, ProcessData data)
        {
            ServerInstance = instance;
            Profile = profile;
            Modlist = modlist;
            ProcessData = data;
            Information = GetInformationFromInit(profile, instance);

            _process = process;
            _process.EnableRaisingEvents = true;
            _process.Exited -= OnProcessExited;
            _process.Exited += OnProcessExited;
        }

        public event EventHandler? ProcessExited;

        public event EventHandler<TrebuchetFailEventArgs>? ProcessFailed;

        public event EventHandler<TrebuchetStartEventArgs>? ProcessStarted;

        public bool Closed { get; private set; }

        public ServerInstanceInformation Information { get; }

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

            if ((LastResponsive + TimeSpan.FromSeconds(Profile.ZombieCheckSeconds)) < DateTime.UtcNow && Profile.KillZombies)
            {
                Log.Write($"Killing zombie instance {ServerInstance} ({_process.Id})", LogSeverity.Warning);
                _process.Kill();
            }
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
            ProcessExited?.Invoke(this, EventArgs.Empty);
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

        private ServerInstanceInformation GetInformationFromInit(ServerProfile profile, int instance)
        {
            string instancePath = profile.GetInstancePath(instance);
            string initPath = Path.Combine(instancePath, string.Format(Config.FileIniServer, "Engine"));
            IniDocument document = IniParser.Parse(Tools.GetFileContent(initPath));

            IniSection section = document.GetSection("OnlineSubsystem");
            string title = section.GetValue("ServerName", "Conan Exile Dedicated Server");

            section = document.GetSection("URL");
            int port;
            if (!int.TryParse(section.GetValue("Port", "7777"), out port))
                port = 7777;

            section = document.GetSection("OnlineSubsystemSteam");
            int queryPort;
            if (!int.TryParse(section.GetValue("GameServerQueryPort", "27015"), out queryPort))
                queryPort = 27015;

            document = IniParser.Parse(Tools.GetFileContent(Path.Combine(instancePath, string.Format(Config.FileIniServer, "Game"))));
            section = document.GetSection("RconPlugin");
            int rconPort;
            if (!int.TryParse(section.GetValue("Port", "25575"), out rconPort))
                rconPort = 25575;

            return new ServerInstanceInformation(instance, title, port, queryPort, rconPort);
        }
    }
}