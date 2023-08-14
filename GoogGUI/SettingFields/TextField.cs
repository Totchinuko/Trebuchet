using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI.SettingFields
{
    public class TextField : Field<string, string>
    {
        public override bool IsDefault => Default == Value;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["TextboxField"];

        protected override string? GetConvert(object? value)
        {
            if (value is not string n) throw new Exception("Value was expected to be a string.");
            return n;
        }

        protected override void ResetToDefault()
        {
            Value = Default;
        }

        protected override object? SetConvert(string? value)
        {
            return value;
        }
    }
}
