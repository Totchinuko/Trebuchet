using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trebuchet.ViewModels.Panels;
using TrebuchetUtils;

namespace Trebuchet.Messages
{
    public class PanelMessage(object? sender) : ITinyMessage
    {
        public object? Sender { get; } = sender;
    }

    public class PanelRefreshConfigMessage() : PanelMessage(null)
    {
    }
}