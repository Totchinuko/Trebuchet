
using Microsoft.Extensions.Logging;
using tot_lib;

namespace TrebuchetLib
{
    public class MixedConsole : IDisposable, ITrebuchetConsole
    {
        private readonly IRcon _rcon;

        public MixedConsole(IRcon rcon)
        {
            _rcon = rcon;
            _rcon.RconResponded += OnRconMessaged;
        }

        public event AsyncEventHandler<ConsoleLog>? Received;

        
        public void Dispose()
        {
            _rcon.RconResponded -= OnRconMessaged;
        }

        public MixedConsole AddLogSource(ILogReader reader, ConsoleLogSource source)
        {
            reader.LogReceived += async (_, args) =>
            {
                if (args.Exception is not null)
                    await OnLogReceived(ConsoleLog.CreateError(args.Output, source));
                else
                {
                    var body = string.IsNullOrEmpty(args.Category) ? args.Output : args.Category + ":" + args.Output;
                    await OnLogReceived(ConsoleLog.Create(body, args.LogLevel, args.Date.ToUniversalTime(), source));
                }
            };
            return this;
        }

        public async Task Send(string data, CancellationToken ct)
        {
            await _rcon.Send(data, ct);
        }

        private async Task OnLogReceived(ConsoleLog log)
        {
            if(Received is not null)
                await Received.Invoke(this, log);
        }

        private async Task OnRconMessaged(object? sender, RconEventArgs e)
        {
            if (e.Exception != null)
                await OnLogReceived(ConsoleLog.CreateError(e.Response, ConsoleLogSource.RCon));
            else if (!string.IsNullOrWhiteSpace(e.Response))
                await OnLogReceived(ConsoleLog.Create(e.Response, LogLevel.Information, ConsoleLogSource.RCon));
        }
    }
}