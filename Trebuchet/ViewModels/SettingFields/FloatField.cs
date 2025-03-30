using System;

namespace Trebuchet.ViewModels.SettingFields
{
    public class FloatField() : Field<float, float>("FloatField")
    {
        public override bool IsDefault => Math.Abs(Value - Default) < Tolerance;
        public float Maximum { get; set; } = float.MaxValue;
        public float Minimum { get; set; } = float.MinValue;
        
        public float Tolerance { get; set; } =  0.001f;

        public override void ResetToDefault()
        {
            Value = Default;
        }

        protected override float GetConvert(object? value)
        {
            if (value is not float f) throw new Exception("Value is not a float");
            return MathF.Min(Maximum, MathF.Max(Minimum, f));
        }

        protected override object SetConvert(float value)
        {
            return value;
        }
    }
}