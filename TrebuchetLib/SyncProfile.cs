using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json.Serialization;
using SteamKit2.CDN;

namespace TrebuchetLib
{
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(SyncProfile))]
    public partial class SyncProfileJsonContext : JsonSerializerContext
    {
    }
    
    public sealed class SyncProfile : ProfileFile<SyncProfile>
    {
        public string SyncURL { get; set; } = string.Empty;
        public List<string> Modlist { get; set; } = [];
        public List<ClientConnection> ClientConnections { get; set; } = [];
    }
}