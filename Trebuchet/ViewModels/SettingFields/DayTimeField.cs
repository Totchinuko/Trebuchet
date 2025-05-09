using System;

namespace Trebuchet.ViewModels.SettingFields;

public class DayTimeField(bool useSeconds) : Field<DayTimeField, TimeSpan>(TimeSpan.Zero)
{
    public bool UseSeconds { get; } = useSeconds;
}