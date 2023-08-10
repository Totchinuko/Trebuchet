using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI.Attributes
{
    public class DirectoryFieldAttribute : FieldAttribute<string>
    {
        private string _defaultValue;

        public DirectoryFieldAttribute(string name, string defaultValue = "") : base(name)
        {
            _defaultValue = defaultValue;
        }

        public override string Template => "DirectoryField";

        protected override string? GetDefault()
        {
            return _defaultValue;
        }

        protected override bool IsDefault(string? value)
        {
            return _defaultValue.Equals(value);
        }
    }
}
