using System;
using System.Collections.Generic;
using System.Linq;
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
        private List<string> _modlist = new List<string>();

        public List<string> Modlist { get => _modlist; set => _modlist = value; }
    }
}
