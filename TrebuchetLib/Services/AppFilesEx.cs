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
}