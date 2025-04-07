using System;

namespace Trebuchet.ViewModels.SettingFields
{
    public class IntSliderField(int minimum, int maximum, int frequency) : 
        Field<IntSliderField, int>(0)
    {
        public int Frequency { get; set; } = frequency;

        public int Maximum { get; set; } = maximum;

        public int Minimum { get; set; } = minimum;

        public bool TickEnabled => Frequency > 0;

    }
}