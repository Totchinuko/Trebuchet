using System;
using Avalonia;
using Avalonia.Controls.Templates;
using ReactiveUI;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.Panels
{
    public class MenuElement(string label) : ReactiveObject
    {
        public string Label { get; } = label;
        
        public virtual void OnWindowShow()
        { }
    }
}