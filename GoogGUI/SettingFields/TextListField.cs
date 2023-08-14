using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI.SettingFields
{
    internal class TextListField : Field<TrulyObservableCollection<ObservableString>, int>
    {
        public override bool IsDefault => Value == null ? false : Value.Count == Default;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["StringListFields"];

        protected override TrulyObservableCollection<ObservableString>? GetConvert(object? value)
        {
            if (value is not List<string> list) throw new Exception("Value was expected to be a List of string.");
            return new TrulyObservableCollection<ObservableString>(list.Select(x => (ObservableString)x));
        }

        protected override void ResetToDefault()
        {
            Value = new TrulyObservableCollection<ObservableString>();
        }

        protected override object? SetConvert(TrulyObservableCollection<ObservableString>? value)
        {
            return value?.Select(x => (string)x).ToList();
        }
    }
}
