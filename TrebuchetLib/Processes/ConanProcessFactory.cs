using Microsoft.Extensions.Logging;

namespace TrebuchetLib.Processes;

public class ConanProcessFactory(ILogger<Rcon> rConLogger, ILogger<LogReader> gameLogger)
{
    public IConanProcessBuilder Create()
    {
        return ConanProcessBuilder.Create()
            .SetRConLogger(rConLogger)
            .SetGameLogger(gameLogger);
    }
}