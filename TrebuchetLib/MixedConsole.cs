
namespace TrebuchetLib
{
    public class MixedConsole : IDisposable, IConsole
    {
        private readonly object _consoleLogLock = new object();
        private List<ConsoleLog> _consoleLog = new List<ConsoleLog>(201);
        private IRcon _rcon;

        public MixedConsole(IRcon rcon)
        {
            _rcon = rcon;
            _rcon.RconResponded += OnRconMessaged;
            _rcon.RconSent += OnRconSent;
        }

        public event EventHandler<ConsoleLogEventArgs>? LogReceived;

        public IEnumerable<ConsoleLog> Historic
        {
            get
            {
                lock (_consoleLogLock)
                    return _consoleLog.ToList();
            }
        }

        public void Dispose()
        {
            _rcon.RconSent -= OnRconMessaged;
            _rcon.RconResponded -= OnRconMessaged;
            _rcon.Cancel();
        }

        public void SendCommand(string data)
        {
            _rcon.Send(data);
        }

        private void AddLog(ConsoleLog log)
        {
            lock (_consoleLogLock)
            {
                _consoleLog.Add(log);
                if (_consoleLog.Count > 200)
                    _consoleLog.RemoveRange(0, _consoleLog.Count - 200);
            }
            LogReceived?.Invoke(this, new ConsoleLogEventArgs(log));
        }

        private void OnRconMessaged(object? sender, RconEventArgs e)
        {
            if (e.Exception != null)
                AddLog(new ConsoleLog(e.Exception.Message, true, true));
            else if (!string.IsNullOrWhiteSpace(e.Response))
                AddLog(new ConsoleLog(e.Response, false, true));
        }

        private void OnRconSent(object? sender, RconEventArgs e)
        {
            if (e.Exception != null)
                AddLog(new ConsoleLog(e.Exception.Message, true, false));
            else if (!string.IsNullOrWhiteSpace(e.Response))
                AddLog(new ConsoleLog(e.Response, false, false));
        }
    }
}