using System;
using Avalonia;
using Avalonia.Controls.Templates;

namespace Trebuchet.ViewModels.Panels
{
    public class MenuElement(string label)
    {
        public string Label { get; } = label;
        
        public virtual void OnWindowShow()
        { }
    }
}