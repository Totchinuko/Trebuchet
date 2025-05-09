using System;

namespace Trebuchet.ViewModels.SettingFields;

public class DurationField(TimeSpan minDuration, TimeSpan maxDuration) 
    : Field<DurationField, TimeSpan>(initialValue:TimeSpan.Zero)
{
    public TimeSpan MinDuration { get; } = minDuration;
    public TimeSpan MaxDuration { get; } = maxDuration;
}