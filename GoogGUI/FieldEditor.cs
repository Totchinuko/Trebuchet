using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public abstract class FieldEditor
    {
        public abstract List<IField> Fields { get; set; }
        public DataTemplate Template => (DataTemplate)Application.Current.Resources["FieldEditor"];
    }
}