using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Trebuchet.SettingFields
{
    public class TextField : Field<string, string>
    {
        public override string? Default
        {
            get
            {
                if (base.Default == null)
                    return string.Empty;
                return base.Default;
            }
            set => base.Default = value;
        }

        public override bool IsDefault => Default == Value;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["TextboxField"];

        public override void ResetToDefault()
        {
            Value = Default;
        }

        protected override string? GetConvert(object? value)
        {
            if (value is not string n) throw new Exception("Value was expected to be a string.");
            return n;
        }

        protected override object? SetConvert(string? value)
        {
            return value;
        }
    }
}