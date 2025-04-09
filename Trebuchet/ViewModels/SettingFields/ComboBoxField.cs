using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Trebuchet.ViewModels.SettingFields
{
    public class ComboBoxField() : Field<ComboBoxField,int>(0)
    {
        public ObservableCollection<string> Options { get; set; } = [];

        public ComboBoxField AddOption(string option)
        {
            Options.Add(option);
            return this;
        }
    }
}