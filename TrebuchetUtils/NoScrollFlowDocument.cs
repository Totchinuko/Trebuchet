using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace TrebuchetUtils
{
    public class NoScrollFlowDocument : FlowDocumentScrollViewer
    {
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
        }

    }
}
