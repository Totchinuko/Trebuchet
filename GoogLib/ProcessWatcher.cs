using Goog;
using System.Diagnostics;

namespace GoogLib
{
    public sealed class ProcessWatcher
    {
        private readonly Config _config;
        private List<Process> _clientProcesses = new List<Process>();
        private List<Process> _serverProcesses = new List<Process>();
        private object theLock = new object();

        public ProcessWatcher(Config config)
        {
            _config = config;
        }

        public int RunningClients => _clientProcesses.Count;

        public int RunningServer => _serverProcesses.Count;

        public void StopServers()
        {
            foreach (Process p in _serverProcesses.ToList())
                p.CloseMainWindow();
        }

        private async Task Watcher(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                _clientProcesses = Process.GetProcesses(Config.FileClientBin).ToList();
                _serverProcesses = Process.GetProcesses(Config.FileServerBin).ToList();

                foreach ()

                    await Task.Delay(1000);
            }
        }
    }
}