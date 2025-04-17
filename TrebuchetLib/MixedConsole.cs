
using tot_lib;

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

        public event AsyncEventHandler<ConsoleLogEventArgs>? LogReceived;

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
            _rcon.RconSent -= OnRconSent;
            _rcon.RconResponded -= OnRconMessaged;
        }

        public async Task SendCommand(string data, CancellationToken ct)
        {
            await _rcon.Send(data, ct);
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

        private Task OnRconMessaged(object? sender, RconEventArgs e)
        {
            if (e.Exception != null)
                AddLog(new ConsoleLog(e.Exception.Message, isError:true, isReceived:true));
            else if (!string.IsNullOrWhiteSpace(e.Response))
                AddLog(new ConsoleLog(e.Response, isError:false, isReceived:true));
            return Task.CompletedTask;
        }

        private Task OnRconSent(object? sender, RconEventArgs e)
        {
            if (e.Exception != null)
                AddLog(new ConsoleLog(e.Exception.Message, isError:true, isReceived:false));
            else if (!string.IsNullOrWhiteSpace(e.Response))
                AddLog(new ConsoleLog(e.Response, isError:false, isReceived:false));
            return Task.CompletedTask;
        }
    }
}