using System;
using System.Reflection;
using System.Windows;

namespace Trebuchet
{
    public class IntField : Field<int, int>
    {
        private int _maximum = int.MaxValue;
        private int _minimum = int.MinValue;

        public int Maximum { get => _maximum; set => _maximum = value; }

        public int Minimum { get => _minimum; set => _minimum = value; }

        public override bool IsDefault => Value == Default;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["IntField"];

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