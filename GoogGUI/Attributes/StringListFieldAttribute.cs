using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI.Attributes
{
    public class StringListFieldAttribute : FieldAttribute<TrulyObservableCollection<ObservableString>>
    {
        public StringListFieldAttribute(string name) : base(name)
        {
        }

        public override string Template => "StringListFields";

        protected override TrulyObservableCollection<ObservableString>? GetDefault()
        {
            return new TrulyObservableCollection<ObservableString>();
        }

        protected override bool IsDefault(TrulyObservableCollection<ObservableString>? value)
        {
            return value == null || value.Count == 0;
        }
    }
}
