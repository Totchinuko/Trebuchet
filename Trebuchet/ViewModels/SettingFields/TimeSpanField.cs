using System;

namespace Trebuchet.ViewModels.SettingFields;

public class TimeSpanField(TimeSpan minDuration, TimeSpan maxDuration) 
    : Field<TimeSpanField, TimeSpan>(initialValue:TimeSpan.Zero)
{
    public TimeSpan MinDuration { get; } = minDuration;
    public TimeSpan MaxDuration { get; } = maxDuration;
}