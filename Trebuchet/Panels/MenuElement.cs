using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Trebuchet.Panels;

namespace Trebuchet.Panels
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