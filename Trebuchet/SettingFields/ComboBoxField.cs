using System;
using System.Collections.Generic;

namespace Trebuchet.SettingFields
{
    public class ComboBoxField() : Field<int, int>("ComboBoxField")
    {
        public override bool IsDefault => Default == Value;

        public List<string> Options { get; set; } = new List<string>();

        public override void ResetToDefault()
        {
            Value = Default;
        }

        protected override int GetConvert(object? value)
        {
            if (value is not int i)
                throw new ArgumentException("Value must be an int", nameof(value));
            return i;
        }

        protected override object? SetConvert(int value)
        {
            return value;
        }
    }
}