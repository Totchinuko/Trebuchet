using System;

namespace Trebuchet.SettingFields
{
    public class IntSliderField() : Field<int, int>("IntSliderField")
    {
        public int Frequency { get; set; } = -1;

        public override bool IsDefault => Value == Default;

        public int Maximum { get; set; } = 100;

        public int Minimum { get; set; } = 0;

        public bool TickEnabled => Frequency > 0;

        public override void ResetToDefault()
        {
            Value = Default;
        }

        protected override int GetConvert(object? value)
        {
            if (value is not int n) throw new Exception("Value was expected to be an int.");
            return Math.Clamp(n, Minimum, Maximum);
        }

        protected override object? SetConvert(int value)
        {
            return Math.Clamp(value, Minimum, Maximum);
        }
    }
}