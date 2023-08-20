using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Trebuchet
{
    [JsonDerivedType(typeof(LogFilterPanel), "LogFilter")]
    [JsonDerivedType(typeof(Dashboard), "Dashboard")]
    [JsonDerivedType(typeof(RconPanel), "Rcon")]
    [JsonDerivedType(typeof(ClientSettings), "ClientSettings")]
    [JsonDerivedType(typeof(ServerSettings), "ServerSettings")]
    [JsonDerivedType(typeof(ModlistHandler), "Modlist")]
    [JsonDerivedType(typeof(Settings), "Settings")]
    public class MenuElement
    {
        public string Label { get; set; } = string.Empty;

    }
}
