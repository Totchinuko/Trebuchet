using System.Text;

namespace TrebuchetLib.Services.Importer;

public class PlainTextImporter(AppSetup setup) : ITrebuchetImporter
{
    public ModlistExport ParseImport(string import)
    {
        var modlist = import.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseLine).ToList();
        return new ModlistExport()
        {
            Modlist = modlist
        };
    }

    public bool CanParseImport(string import)
    {
        var lines = import.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            try
            {
                ParseLine(line);
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    public string Export(ModListProfile profile)
    {
        StringBuilder builder = new();
        foreach (var mod in profile.Modlist)
        {
            if (!setup.TryGetModPath(mod, out var path))
                throw new Exception($"Could not resolve mod {mod}");
            builder.AppendLine(path);
        }

        return builder.ToString();
    }

    private string ParseLine(string line)
    {
        ulong modId;
        if (ulong.TryParse(line, out modId))
            return modId.ToString();
            
        var file = line.Trim();
        if (file.StartsWith("*"))
            file = file.Substring(1);
            
        if (Path.GetExtension(file) != "."+Constants.PakExt) 
            throw new IOException($"modlist file contain unsupported format {file}");

        var parentDir = Directory.GetParent(file)?.Name ?? string.Empty;
        if (ulong.TryParse(parentDir, out modId))
            return modId.ToString();
        
        var fileName = Path.GetFileNameWithoutExtension(file);
        if (ulong.TryParse(fileName, out modId))
            return modId.ToString();

        return file;
    }
}