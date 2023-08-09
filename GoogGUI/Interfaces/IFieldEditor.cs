using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogGUI
{
    public interface IFieldEditor
    {
        public List<IField> Fields { get; }

        public List<RequiredCommand> RequiredActions { get; }

    }
}
