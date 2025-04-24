using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json.Serialization;

namespace TrebuchetLib
{
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(ModListProfile))]
    public partial class ModListProfileJsonContext : JsonSerializerContext
    {
    }
    
    public sealed class ModListProfile : ProfileFile<ModListProfile>
    {
        public List<string> Modlist { get; set; } = [];

        public IEnumerable<ulong> GetWorkshopMods()
        {
            foreach (var mod in Modlist)
            {
                if(ulong.TryParse(mod, out var result))
                    yield return result;
            }
        }
    }
}