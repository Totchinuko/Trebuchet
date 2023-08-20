using System;
using System.Reflection;
using System.Windows;

namespace Trebuchet
{
    public class IntSliderField : Field<int, int>
    {
        private int _maximum = 100;
        private int _minimum = 0;
        private int _frequency = -1;

        public int Maximum { get => _maximum; set => _maximum = value; }

        public int Minimum { get => _minimum; set => _minimum = value; }

        public bool TickEnabled => _frequency > 0;

        public int Frequency { get => _frequency; set => _frequency = value; }

        public override bool IsDefault => Value == Default;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["IntSliderField"];

        protected override int GetConvert(object? value)
        {
            if (value is not int n) throw new Exception("Value was expected to be an int.");
            return Math.Clamp(n, Minimum, Maximum);
        }

        protected override void ResetToDefault()
        {
            Value = Default;
        }

        protected override object? SetConvert(int value)
        {
            return Math.Clamp(value, Minimum, Maximum);
        }
    }
}