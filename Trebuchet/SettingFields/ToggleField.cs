using System;

namespace Trebuchet.SettingFields
{
    public class ToggleField() : Field<bool, bool>("ToggleField")
    {
        public override bool DisplayGenericDescription => false;

        public override bool IsDefault => Default == Value;

        public override void ResetToDefault()
        {
            Value = Default;
        }

        protected override bool GetConvert(object? value)
        {
            if (value is not bool n) throw new Exception("Value was expected to be a boolean.");
            return n;
        }

        protected override object SetConvert(bool value)
        {
            return value;
        }
    }
}