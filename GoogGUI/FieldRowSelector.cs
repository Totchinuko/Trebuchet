using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GoogGUI
{
    public class FieldRowSelector : DataTemplateSelector
    {

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if(item is not Field field) throw new ArgumentException("item is not a Field");
            if (field.UseFieldRow)
                return (DataTemplate)Application.Current.Resources["FieldRow"];
            else
                return field.Template;
        }
    }
}
