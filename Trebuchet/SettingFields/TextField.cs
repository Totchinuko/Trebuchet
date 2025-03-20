using System;

namespace Trebuchet.SettingFields
{
    public class TextField(string template) : Field<string, string>(string.IsNullOrEmpty(template) ? "TextboxField" : template)
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

        public override void ResetToDefault()
        {
            Value = Default;
        }

        protected override string GetConvert(object? value)
        {
            if (value is not string n) throw new Exception("Value was expected to be a string.");
            return n;
        }

        protected override object? SetConvert(string? value)
        {
            return value;
        }

        public TextField() : this("TextboxField")
        {
        }
    }
}