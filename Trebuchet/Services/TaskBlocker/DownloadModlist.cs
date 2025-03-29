using System;

namespace Trebuchet.Services.TaskBlocker;

public class DownloadModlist(string label) : IBlockedTaskType
{
    public string Label { get; } = label;
    public Type[] CancellingTypes { get; } = [];
    public Type[] BlockingTypes { get; } = [typeof(SteamDownload)];
}