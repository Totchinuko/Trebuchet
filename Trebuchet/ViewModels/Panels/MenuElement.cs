using System;
using Avalonia;
using Avalonia.Controls.Templates;

namespace Trebuchet.ViewModels.Panels
{
    public class MenuElement(string label, string template)
    {
        public string Label { get; } = label;
        
        public IDataTemplate Template {
            get
            {
                if(Application.Current == null) throw new Exception("Application.Current is null");

                if (Application.Current.Resources.TryGetResource(template, Application.Current.ActualThemeVariant,
                        out var resource) && resource is IDataTemplate dataTemplate)
                {
                    return dataTemplate;
                }

                throw new Exception($"Template {template} not found");
            }
        }

        public virtual void OnWindowShow()
        { }
    }
}