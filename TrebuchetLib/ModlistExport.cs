using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TrebuchetLib
{
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(ModlistExport))]
    public partial class ModlistExportJsonContext : JsonSerializerContext
    {
    }
    
    public class ModlistExport
    {
        public List<string> Modlist { get; set; } = [];

        public void GetValues(ModListProfile profile)
        {
            Modlist = profile.Modlist.ToList();
        }

        public void SetValues(ModListProfile profile)
        {
            profile.Modlist = Modlist.ToList();
        }
    }
}
