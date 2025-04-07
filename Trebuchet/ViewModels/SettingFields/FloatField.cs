using System;

namespace Trebuchet.ViewModels.SettingFields
{
    public class FloatField(float minimum, float maximum, float tolerance = 0.001f) : 
        Field<FloatField, float>(0f)
    {
        public float Maximum { get; set; } = maximum;
        public float Minimum { get; set; } = minimum;
        
        public float Tolerance { get; set; } = tolerance;

        protected override bool IsValueDefault(float value, float defValue)
        {
            return Math.Abs(value - defValue) < Tolerance;
        }
    }
}