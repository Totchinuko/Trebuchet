﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.SettingFields
{
    internal class TextListField() : Field<TextListField, ObservableCollection<string>>([])
    {
        protected override bool AreValuesEqual(ObservableCollection<string> valueA, ObservableCollection<string> valueB)
        {
            return valueB.SequenceEqual(valueA);
        }
    }
}