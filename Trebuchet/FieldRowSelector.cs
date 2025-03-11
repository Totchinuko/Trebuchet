using System;

namespace Trebuchet
{
    public class FieldRowSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is not Field field) throw new ArgumentException("item is not a Field");
            if (field.UseFieldRow)
                return (DataTemplate)Application.Current.Resources["FieldRow"];
            else
                return field.Template;
        }
    }
}