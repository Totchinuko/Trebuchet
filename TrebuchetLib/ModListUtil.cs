namespace TrebuchetLib;

public static class ModListUtil
{
    public static bool TryParseDirectory2ModId(string path, out ulong id)
    {
        id = 0;
        if (ulong.TryParse(Path.GetFileName(path), out id))
            return true;

        string? parent = Path.GetDirectoryName(path);
        if (parent != null && ulong.TryParse(Path.GetFileName(parent), out id))
            return true;

        return false;
    }

    public static bool TryParseFile2ModId(string path, out ulong id)
    {
        id = 0;
        string? folder = Path.GetDirectoryName(path);
        if (folder == null)
            return false;
        if (ulong.TryParse(Path.GetFileName(folder), out id))
            return true;

        return false;
    }

    public static bool TryParseModId(string path, out ulong id)
    {
        id = 0;
        if (ulong.TryParse(path, out id))
            return true;

        if (Path.GetExtension(path) == ".pak")
            return TryParseFile2ModId(path, out id);
        else
            return TryParseDirectory2ModId(path, out id);
    }
}