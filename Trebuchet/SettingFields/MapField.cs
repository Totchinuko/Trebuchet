using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Trebuchet.SettingFields
{
    public class MapField : TextField
    {
        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["MapField"];
    }
}