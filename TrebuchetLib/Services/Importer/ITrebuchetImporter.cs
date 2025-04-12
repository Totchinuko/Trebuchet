namespace TrebuchetLib.Services.Importer;

public interface ITrebuchetImporter
{
    IEnumerable<string> ParseImport(string import);
    bool CanParseImport(string import);
    string Export(IEnumerable<string> modlist);
}