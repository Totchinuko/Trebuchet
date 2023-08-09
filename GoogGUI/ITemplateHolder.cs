using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public interface ITemplateHolder
    {
        public DataTemplate Template { get; }
    }
}
