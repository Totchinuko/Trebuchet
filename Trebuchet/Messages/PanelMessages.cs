﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trebuchet.Panels;
using TrebuchetUtils;

namespace Trebuchet.Messages
{
    public class PanelMessage(object? sender) : ITinyMessage
    {
        public object? Sender { get; } = sender;
    }
    
    public class PanelActivateMessage(object? sender, Panel panel) : PanelMessage(sender)
    {
        public Panel Panel { get; } = panel;
    }



    public class PanelRefreshConfigMessage() : PanelMessage(null)
    {
    }
}