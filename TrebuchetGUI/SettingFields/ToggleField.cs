using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Trebuchet.SettingFields
{
    public class ToggleField : Field<bool, bool>
    {
        public override bool IsDefault => Default == Value;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["ToggleField"];

        public override bool DisplayGenericDescription => false;

        protected override bool GetConvert(object? value)
        {
            if (value is not bool n) throw new Exception("Value was expected to be a boolean.");
            return n;
        }

        protected override void ResetToDefault()
        {
            Value = Default;
        }

        protected override object? SetConvert(bool value)
        {
            return value;
        }
    }
}
