using System;
using System.Collections.Generic;
using System.Linq;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.SettingFields
{
    internal class TextListField() : Field<TrulyObservableCollection<ObservableString>, List<string>>("StringListFields")
    {
        public override bool IsDefault
        {
            get
            {
                if (Value == null) return Default == null;
                if (Default == null) return Value == null;

                return Default.SequenceEqual(Value.Select(x => (string)x));
            }
        }

        public override void ResetToDefault()
        {
            if (Default == null)
            {
                Value = null;
                return;
            }
            Value = new TrulyObservableCollection<ObservableString>(Default.Select(x => (ObservableString)x));
        }

        protected override TrulyObservableCollection<ObservableString>? GetConvert(object? value)
        {
            if (value is not List<string> list) throw new Exception("Value was expected to be a List of string.");
            return new TrulyObservableCollection<ObservableString>(list.Select(x => (ObservableString)x));
        }

        protected override object? SetConvert(TrulyObservableCollection<ObservableString>? value)
        {
            return value?.Select(x => (string)x).ToList();
        }
    }
}