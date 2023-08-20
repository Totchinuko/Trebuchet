using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Trebuchet
{
    public class TemplateHolderSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            // Null value can be passed by IDE designer
            if (item == null) return null;
            if (item is not ITemplateHolder holder) return null;
            return holder.Template;            
        }
    }
}
