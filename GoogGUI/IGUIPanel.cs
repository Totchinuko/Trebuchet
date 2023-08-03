using Goog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogGUI
{
    public interface IGUIPanel
    {
        void Setup(Config config, Profile? profile);
        void Close();
    }
}
