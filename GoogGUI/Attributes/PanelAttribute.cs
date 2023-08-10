using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class PanelAttribute : Attribute
    {
        private bool _bottom;
        private int _sort;
        private string _icon;
        private string _label;
        private string _template;

        public PanelAttribute(string label, string icon, bool bottom, int sort, string template = "")
        {
            _bottom = bottom;
            _sort = sort;
            _icon = icon;
            _label = label;
            _template = template;
        }

        public bool Bottom => _bottom;
        public int Sort => _sort;
        public string Icon => _icon;
        public string Label => _label;
        public string Template => _template;
    }
}
