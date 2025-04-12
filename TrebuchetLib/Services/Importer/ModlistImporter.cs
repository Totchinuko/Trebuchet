using Trebuchet.ViewModels;

namespace TrebuchetLib.Services.Importer;

public class ModlistImporter
{

    public ModlistImporter(AppModlistFiles files)
    {
        _files = files;
        _importers.Add(ImportFormats.Json, new JsonImporter());
        _importers.Add(ImportFormats.Txt, new PlainTextImporter(files));
    }
    
    private readonly AppModlistFiles _files;
    private Dictionary<ImportFormats, ITrebuchetImporter> _importers = [];

    public ImportFormats GetFormat(string data)
    {
        foreach (var importer in _importers)
            if (importer.Value.CanParseImport(data))
                return importer.Key;

        return ImportFormats.Invalid;
    }

    public string Export(IEnumerable<string> data, ImportFormats format)
    {
        if (!_importers.TryGetValue(format, out var importer))
            throw new TrebException($"No importer for the given format {format}");
        return importer.Export(data);
    }

    public IEnumerable<string> Import(string data)
    {
        foreach (var importer in _importers.Values)
        {
            try
            {
                return importer.ParseImport(data);
            }
            catch{continue;}
        }

        throw new TrebException("Could not import the provided data with any importers");
    }

    public IEnumerable<string> Import(string data, ImportFormats format)
    {
        if (!_importers.TryGetValue(format, out var importer))
            throw new TrebException($"No importer for the given format {format}");
        return importer.ParseImport(data);
    }

}
