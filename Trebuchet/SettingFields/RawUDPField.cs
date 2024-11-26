using System;
using System.Windows;

namespace Trebuchet.SettingFields
{
    public class RawUDPField : Field<int, int>
    {
        public override bool IsDefault => true;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["IntField"];

        public override int Value
        {
            get
            {
                var value = base.Value;
                return value + 2;
            }
            set { }
        }

        public override void RefreshVisibility()
        {
            base.RefreshVisibility();
            OnPropertyChanged(nameof(Value));
        }

        public override void ResetToDefault()
        {
        }

        protected override int GetConvert(object? value)
        {
            if (value is not int n) throw new Exception("Value was expected to be an int.");
            return n;
        }

        protected override object? SetConvert(int value)
        {
            return value;
        }
    }
}