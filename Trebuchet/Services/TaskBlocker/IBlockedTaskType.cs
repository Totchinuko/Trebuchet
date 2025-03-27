using System;

namespace Trebuchet.Services.TaskBlocker;

public interface IBlockedTaskType
{
    string Label { get; }
    Type[] BlockingTypes { get; }
    Type[] CancellingTypes { get; }
}