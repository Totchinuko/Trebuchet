using System.Text.Json;

namespace TrebuchetLib.Services.Importer;

public class JsonImporter : ITrebuchetImporter
{
    public ModlistExport ParseImport(string import)
    {
        var data = JsonSerializer.Deserialize(import, ModlistExportJsonContext.Default.ModlistExport);
        if (data is null)
            throw new JsonException("Imported data is invalid");
        data.Modlist = data.Modlist.Select(ParseMod).ToList();
        return data;
    }

    public bool CanParseImport(string import)
    {
        try
        {
            var data = JsonSerializer.Deserialize(import, ModlistExportJsonContext.Default.ModlistExport);
            if (data is null) return false;
            foreach (var mod in data.Modlist)
            {
                ParseMod(mod);
            }
        }
        catch
        {
            return false;
        }

        return true;
    }

    public string Export(ModListProfile profile)
    {
        var export = new ModlistExport();
        export.GetValues(profile);
        return JsonSerializer.Serialize(export, ModlistExportJsonContext.Default.ModlistExport);
    }

    private string ParseMod(string entry)
    {
        if (ulong.TryParse(entry, out var modId))
            return modId.ToString();
        
        var file = entry.Trim();
        if (file.StartsWith("*"))
            file = file.Substring(1);

        if (Path.GetExtension(file) != "."+Constants.PakExt) 
            throw new IOException($"modlist file contain unsupported format {file}");

        return file;
    }
}