using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI.Attributes
{
    public class IntSliderFieldAttribute : FieldAttribute<int>
    {
        private double _min;
        private double _max;
        private double _frequency = -1;
        private int _defaultValue = 0;

        public IntSliderFieldAttribute(string name, double min, double max, int defaultValue = 0) : base(name)
        {
            _min = min;
            _max = max;
            _defaultValue = defaultValue;
        }

        public double Min => _min;
        public double Max => _max;

        public double Frequency { get => _frequency; set => _frequency = value; }

        public override string Template => "IntSliderField";

        public override IField ConstructField(object target, PropertyInfo property)
        {
            return new SliderField<int>(_name, property, target, Template)
                .WithMinMax(_min, _max)
                .WithFrequency(_frequency)
                .WithDefault(IsDefault, GetDefault);
        }

        protected override int GetDefault()
        {
            return _defaultValue;
        }

        protected override bool IsDefault(int value)
        {
            return value == _defaultValue;
        }
    }
}
