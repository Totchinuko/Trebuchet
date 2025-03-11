using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trebuchet.SettingFields
{
    public class DirectoryField : TextField
    {
        public bool CreateDefaultFolder { get; set; } = false;

        public string DefaultFolder { get; set; } = string.Empty;

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["DirectoryField"];
    }
}