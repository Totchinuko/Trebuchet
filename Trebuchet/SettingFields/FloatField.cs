using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Trebuchet
{
    public class FloatField : Field<float, float>
    {
        public float Minimum {  get; set; } = float.MinValue;
        public float Maximum { get; set; } = float.MaxValue;

        public override bool IsDefault => Value == Default;

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
