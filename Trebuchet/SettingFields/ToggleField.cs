using System;
using System.Windows;

namespace Trebuchet.SettingFields
{
    public class ToggleField : Field<bool, bool>
    {
        public override bool DisplayGenericDescription => false;

        public override bool IsDefault => Default == Value;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["ToggleField"];

        public override void ResetToDefault()
        {
            Value = Default;
        }

        protected override bool GetConvert(object? value)
        {
            if (value is not bool n) throw new Exception("Value was expected to be a boolean.");
            return n;
        }

        protected override object? SetConvert(bool value)
        {
            return value;
        }
    }
}