using System.Diagnostics;

namespace TrebuchetLib.Processes;

public class ConanServerProcessInfos
{
    public required Process Process { get; init; }
    public required DateTime Start { get; init; }
    public required int Instance { get; init; }
    public required string GameLogs { get; init; }
}