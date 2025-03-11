﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trebuchet
{
    public class TabTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Panel)
                return (DataTemplate)Application.Current.Resources["TabButtonTemplate"];
            else
                return (DataTemplate)Application.Current.Resources["TabLabel"];
        }
    }
}
