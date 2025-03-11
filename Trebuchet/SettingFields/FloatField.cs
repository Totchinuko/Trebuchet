using System;

namespace Trebuchet
{
    public class FloatField : Field<float, float>
    {
        public override bool IsDefault => Value == Default;
        public float Maximum { get; set; } = float.MaxValue;
        public float Minimum { get; set; } = float.MinValue;
        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["FloatField"];

        public override void ResetToDefault()
        {
            Value = Default;
        }

        protected override float GetConvert(object? value)
        {
            if (value is not float f) throw new Exception("Value is not a float");
            return MathF.Min(Maximum, MathF.Max(Minimum, f));
        }

        protected override object? SetConvert(float value)
        {
            return value;
        }
    }
}