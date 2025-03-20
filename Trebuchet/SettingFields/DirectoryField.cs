using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trebuchet.SettingFields
{
    public class DirectoryField() : TextField("DirectoryField")
    {
        public bool CreateDefaultFolder { get; set; } = false;

        public string DefaultFolder { get; set; } = string.Empty;

    }
}