using System;
using System.Reflection;

namespace GoogGUI
{
    public class IntField<T> : Field<T>
    {
        private int _maximum = int.MaxValue;
        private int _minimum = int.MinValue;

        public IntField(string name, PropertyInfo property, object target, string template) : base(name, property, target, template)
        {
        }

        public int Maximum { get => _maximum; set => _maximum = value; }

        public int Minimum { get => _minimum; set => _minimum = value; }

        public IntField<T> WithMinMax(int min, int max)
        {
            _minimum = min;
            _maximum = max;
            return this;
        }
    }
}