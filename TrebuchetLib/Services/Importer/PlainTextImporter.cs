using System.Text;

namespace TrebuchetLib.Services.Importer;

public class PlainTextImporter(AppModlistFiles files) : ITrebuchetImporter
{
    public IEnumerable<string> ParseImport(string import)
    {
        return import.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseLine);
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

    public string Export(IEnumerable<string> modlist)
    {
        StringBuilder builder = new();
        string path;
        foreach (var mod in modlist)
        {
            path = mod;
            if (!files.ResolveMod(ref path))
                throw new TrebException($"Could not resolve mod {mod}");
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