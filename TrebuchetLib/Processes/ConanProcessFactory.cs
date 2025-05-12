using Microsoft.Extensions.Logging;
using TrebuchetLib.Services;

namespace TrebuchetLib.Processes;

public class ConanProcessFactory(ILogger<Rcon> rConLogger, ILogger<LogReader> gameLogger, UserDefinedNotifications notifications)
{
    public IConanProcessBuilder Create()
    {
        return ConanProcessBuilder.Create()
            .SetRConLogger(rConLogger)
            .SetGameLogger(gameLogger)
            .SetUserDefinedNotifications(notifications);
    }
}