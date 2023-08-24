using SteamKit2.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            _rcon.RconSent += OnRconMessaged;
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

        private void OnRconMessaged(object? sender, RconEventArgs e)
        {
            lock (_consoleLogLock)
            {
                ConsoleLog log;
                if (e.Id == -1 && e.Exception != null)
                    log = new ConsoleLog(e.Exception.Message, e.Id);
                else if (!string.IsNullOrWhiteSpace(e.Response))
                    log = new ConsoleLog(e.Response, e.Id);
                else return;

                _consoleLog.Add(log);
                LogReceived?.Invoke(this, new ConsoleLogEventArgs(log));
                if (_consoleLog.Count > 200)
                    _consoleLog.RemoveRange(0, _consoleLog.Count - 200);
            }
        }
    }
}