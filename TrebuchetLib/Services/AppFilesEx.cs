using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace TrebuchetLib.Services;

public static class AppFilesEx
{
    public static string Resolve<T>(this IAppFileHandler<T> handler, string name) where T : JsonFile<T>
    {
        if (handler.Exists(name)) return name;
        return handler.GetDefault();
    }

    public static string GetDirectory<T>(this IAppFileHandler<T> handler, string name) where T : JsonFile<T>
    {
        return Path.GetDirectoryName(handler.GetPath(name)) ?? throw new DirectoryNotFoundException();
    }

    public static bool TryGet<T>(this IAppFileHandler<T> handler, string name, [NotNullWhen(true)] out T? file)
        where T : JsonFile<T>
    {
        file = null;
        if (!handler.Exists(name)) return false;
        try
        {
            file = handler.Get(name);
            return true;
        }
        catch
        {
            return false;
        }
    }
}