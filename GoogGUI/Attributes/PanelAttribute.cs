using System;

namespace GoogGUI.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class PanelAttribute : Attribute
    {
        private bool _bottom;
        private string _group;
        private string _icon;
        private string _label;
        private int _sort;
        private string _template;

        public PanelAttribute(string label, string icon, bool bottom, int sort, string template = "", string group = "")
        {
            _bottom = bottom;
            _sort = sort;
            _icon = icon;
            _label = label;
            _template = template;
            _group = group;
        }

        public bool Bottom => _bottom;

        public string Group => _group;

        public string Icon => _icon;

        public string Label => _label;

        public int Sort => _sort;

        public string Template => _template;
    }
}