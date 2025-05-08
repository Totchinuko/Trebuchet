using Trebuchet.ViewModels;

namespace TrebuchetLib.Services.Importer;

public class ModlistImporter
{

    public ModlistImporter(AppSetup setup)
    {
        _importers.Add(ImportFormats.Json, new JsonImporter());
        _importers.Add(ImportFormats.Txt, new PlainTextImporter(setup));
    }
    
    private Dictionary<ImportFormats, ITrebuchetImporter> _importers = [];

    public ImportFormats GetFormat(string data)
    {
        foreach (var importer in _importers)
            if (importer.Value.CanParseImport(data))
                return importer.Key;

        return ImportFormats.Invalid;
    }

    public string Export(ModListProfile profile, ImportFormats format)
    {
        if (!_importers.TryGetValue(format, out var importer))
            throw new Exception($"No importer for the given format {format}");
        return importer.Export(profile);
    }

    public ModlistExport Import(string data)
    {
        foreach (var importer in _importers.Values)
        {
            try
            {
                return importer.ParseImport(data);
            }
            catch{continue;}
        }

        throw new Exception("Could not import the provided data with any importers");
    }

    public ModlistExport Import(string data, ImportFormats format)
    {
        if (!_importers.TryGetValue(format, out var importer))
            throw new Exception($"No importer for the given format {format}");
        return importer.ParseImport(data);
    }

}
