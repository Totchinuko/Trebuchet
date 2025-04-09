using System;

namespace Trebuchet.Services.TaskBlocker;

public class SteamDownload(string label) : IBlockedTaskType
{
    public string Label { get; } = label;
    public Type[] CancellingTypes { get; } = [typeof(ServersRunning), typeof(ClientRunning)];
    public Type[] BlockingTypes { get; } = [];
}