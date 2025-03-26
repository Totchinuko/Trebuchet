using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TrebuchetLib
{
    public sealed class ModListProfile : ProfileFile<ModListProfile>
    {
        public List<string> Modlist { get; set; } = new List<string>();
        public string SyncURL { get; set; } = string.Empty;

        [JsonIgnore]
        public string ProfileName => Path.GetFileNameWithoutExtension(FilePath) ?? string.Empty;

        public string GetModList()
        {
            return string.Join("\r\n", Modlist);
        }
        
        public void SetModList(string modlist)
        {
            Modlist = modlist.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}