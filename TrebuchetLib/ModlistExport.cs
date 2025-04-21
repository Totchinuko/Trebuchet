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
        public string ServerAddress { get; set; } = string.Empty;
        public int ServerPort { get; set; } = 0;
        public string ServerPassword { get; set; } = string.Empty;

        public void GetValues(ModListProfile profile)
        {
            Modlist = profile.Modlist.ToList();
            ServerAddress = profile.ServerAddress;
            ServerPort = profile.ServerPort;
            ServerPassword = profile.ServerPassword;
        }

        public void SetValues(ModListProfile profile)
        {
            profile.Modlist = Modlist.ToList();
            profile.ServerAddress = ServerAddress;
            profile.ServerPort = ServerPort;
            profile.ServerPassword = ServerPassword;
        }
    }
}
