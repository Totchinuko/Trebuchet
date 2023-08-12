using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI.Attributes
{
    public class MapFieldAttribute : DirectoryFieldAttribute
    {
        public MapFieldAttribute(string name, string defaultValue = "") : base(name, defaultValue)
        {
        }

        public override string Template => "MapField";
    }
}
