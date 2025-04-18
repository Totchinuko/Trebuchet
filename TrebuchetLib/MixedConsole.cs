
using Microsoft.Extensions.Logging;
using SteamKit2.Internal;
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

        public event AsyncEventHandler<ConsoleLogArgs>? Received;

        
        public void Dispose()
        {
            _rcon.RconResponded -= OnRconMessaged;
        }

        public MixedConsole AddLogSource(ILogReader reader, ConsoleLogSource source)
        {
            reader.LogReceived += async (_, args) =>
            {
                if (Received is null) return;
                
                var logArgs = new ConsoleLogArgs();
                foreach (var line in args.Lines)
                {
                    if (line.Exception is not null)
                        logArgs.Append(ConsoleLog.CreateError(line.Output, source));
                    else
                    {
                        var body = string.IsNullOrEmpty(line.Category) ? line.Output : line.Category + ":" + line.Output;
                        logArgs.Append(ConsoleLog.Create(body, line.LogLevel, line.Date.ToUniversalTime(), source));
                    }
                }

                if (logArgs.Logs.Count > 0)
                    await Received.Invoke(this, logArgs);
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
                await Received.Invoke(this, new ConsoleLogArgs().Append(log));
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