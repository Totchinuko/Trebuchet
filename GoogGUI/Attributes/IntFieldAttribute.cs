using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI.Attributes
{
    public class IntFieldAttribute : FieldAttribute<int>
    {
        private int _min;
        private int _max;
        private int _defaultValue = 0;

        public IntFieldAttribute(string name, int min = int.MinValue, int max = int.MaxValue, int defaultValue = 0) : base(name)
        {
            _min = min;
            _max = max;
            _defaultValue = defaultValue;
        }

        public int Min => _min;
        public int Max => _max;

        public override string Template => "IntField";

        public override IField ConstructField(object target, PropertyInfo property)
        {
            return new IntField<int>(_name, property, target, Template)
                .WithMinMax(_min, _max)
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
