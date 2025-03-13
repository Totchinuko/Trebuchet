using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Trebuchet.Panels;

namespace Trebuchet
{
    [JsonDerivedType(typeof(LogFilterPanel), "LogFilter")]
    [JsonDerivedType(typeof(DashboardPanel), "Dashboard")]
    [JsonDerivedType(typeof(RconPanel), "Rcon")]
    [JsonDerivedType(typeof(ClientProfilePanel), "ClientSettings")]
    [JsonDerivedType(typeof(ServerProfilePanel), "ServerSettings")]
    [JsonDerivedType(typeof(ModlistPanel), "Modlist")]
    [JsonDerivedType(typeof(SettingsPanel), "Settings")]
    public class MenuElement
    {
        public string Label { get; set; } = string.Empty;

        public virtual void OnWindowShow()
        { }
    }
}