using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI.Attributes
{
    public class ToggleFieldAttribute : FieldAttribute<bool>
    {
        private bool _defaultValue;

        public ToggleFieldAttribute(string name, bool defaultValue = false) : base(name)
        {
            _defaultValue = defaultValue;
        }

        public override string Template => "ToggleField";

        protected override bool GetDefault()
        {
            return _defaultValue;
        }

        protected override bool IsDefault(bool value)
        {
            return value.Equals(_defaultValue);
        }
    }
}
