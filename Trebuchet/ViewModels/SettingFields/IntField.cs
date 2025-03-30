using System;

namespace Trebuchet.ViewModels.SettingFields
{
    public class IntField() : Field<int, int>("IntField")
    {
        private int _maximum = int.MaxValue;
        private int _minimum = int.MinValue;

        public override bool IsDefault => Value == Default;

        public int Maximum { get => _maximum; set => _maximum = value; }

        public int Minimum { get => _minimum; set => _minimum = value; }

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