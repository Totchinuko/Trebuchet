using System.Reflection;

namespace GoogGUI
{
    public class SliderField<T> : Field<T>
    {
        private double _maximum = 100;
        private double _minimum = 0;
        private bool _tickEnabled = false;
        private double _tickFrequency = 1d;

        public SliderField(string name, PropertyInfo property, object target, string template) : base(name, property, target, template)
        {
        }

        public double Maximum { get => _maximum; set => _maximum = value; }

        public double Minimum { get => _minimum; set => _minimum = value; }

        public bool TickEnabled { get => _tickEnabled; set => _tickEnabled = value; }

        public double TickFrequency { get => _tickFrequency; set => _tickFrequency = value; }

        public SliderField<T> WithMinMax(double min, double max)
        {
            _minimum = min;
            _maximum = max;
            return this;
        }

        public SliderField<T> WithFrequency(double frequency)
        {
            if (frequency < 0) return this;
            _tickEnabled = true;
            _tickFrequency = frequency;
            return this;
        }

        public SliderField<T> WithIntFrequency()
        {
            return WithFrequency(1d);
        }
    }
}