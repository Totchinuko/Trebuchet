namespace TrebuchetLib.Services.Importer;

public interface ITrebuchetImporter
{
    ModlistExport ParseImport(string import);
    bool CanParseImport(string import);
    string Export(ModListProfile profile);
}