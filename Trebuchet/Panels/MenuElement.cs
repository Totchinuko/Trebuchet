using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Templates;
using Trebuchet.Panels;

namespace Trebuchet.Panels
{
    [JsonDerivedType(typeof(LogFilterPanel), "LogFilter")]
    [JsonDerivedType(typeof(DashboardPanel), "Dashboard")]
    [JsonDerivedType(typeof(RconPanel), "Rcon")]
    [JsonDerivedType(typeof(ClientProfilePanel), "ClientSettings")]
    [JsonDerivedType(typeof(ServerProfilePanel), "ServerSettings")]
    [JsonDerivedType(typeof(ModlistPanel), "Modlist")]
    [JsonDerivedType(typeof(SettingsPanel), "Settings")]
    public class MenuElement(string template, string menuTemplate)
    {
        public MenuElement() : this(string.Empty, "TabLabel")
        {
        }
        
        public string Label { get; set; } = string.Empty;
        
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
        
        public IDataTemplate MenuTemplate {
            get
            {
                if(Application.Current == null) throw new Exception("Application.Current is null");

                if (Application.Current.Resources.TryGetResource(menuTemplate, Application.Current.ActualThemeVariant,
                        out var resource) && resource is IDataTemplate dataTemplate)
                {
                    return dataTemplate;
                }

                throw new Exception($"Template {menuTemplate} not found");
            }
        }

        public virtual void OnWindowShow()
        { }
    }
}