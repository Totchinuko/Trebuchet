using System.Diagnostics;

namespace TrebuchetLib.Processes;

public class ConanClientProcessInfos
{
    public required Process Process { get; init; }
    public required DateTime Start { get; init; }
}