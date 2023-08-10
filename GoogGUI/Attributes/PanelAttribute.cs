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

        public PanelAttribute(bool bottom, int sort)
        {
            _bottom = bottom;
            _sort = sort;
        }

        public bool Bottom => _bottom;
        public int Sort => _sort;
    }
}
