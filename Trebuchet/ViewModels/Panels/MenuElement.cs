using System;
using Avalonia;
using Avalonia.Controls.Templates;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.Panels
{
    public class MenuElement(string label) : BaseViewModel
    {
        public string Label { get; } = label;
        
        public virtual void OnWindowShow()
        { }
    }
}