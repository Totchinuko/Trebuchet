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
        public string SyncURL { get; set; } = string.Empty;

        [JsonIgnore]
        public string ProfileName => Path.GetFileNameWithoutExtension(FilePath) ?? string.Empty;

        public string GetModList()
        {
            return string.Join("\r\n", Modlist);
        }

        public IEnumerable<ulong> GetWorkshopMods()
        {
            foreach (var mod in Modlist)
            {
                if(ulong.TryParse(mod, out var result))
                    yield return result;
            }
        }
        
        public void SetModList(string modlist)
        {
            Modlist = modlist.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}