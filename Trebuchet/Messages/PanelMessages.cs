using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trebuchet.Messages
{
    public class PanelActivateMessage : PanelMessage
    {
        public readonly Panel panel;

        public PanelActivateMessage(Panel panel)
        {
            this.panel = panel;
        }
    }

    public class PanelMessage
    {
    }

    public class PanelRefreshConfigMessage : PanelMessage
    {
    }
}