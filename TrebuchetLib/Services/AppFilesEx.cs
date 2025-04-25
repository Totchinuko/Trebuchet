using System.Diagnostics.CodeAnalysis;

namespace TrebuchetLib.Services;

public static class AppFilesEx
{
    public static TRef Resolve<T, TRef>(this IAppFileHandler<T, TRef> handler, TRef reference) 
        where T : JsonFile<T> 
        where TRef : class,IPRef<T, TRef>
    {
        if (handler.Exists(reference)) return reference;
        return handler.GetDefault();
    }
    
    public static bool TryParseModListRef(this AppFiles files, string name, [NotNullWhen(true)] out IPRefWithModList? reference)
    {
        try
        {
            var uri = new Uri(name);
            if (uri.Scheme == Constants.UriScheme)
                return files.TryParseModListRef(uri, out reference);
            reference = null;
            return false;
        }
        catch
        {
            reference = null;
            return false;
        }
    }

    public static IPRefWithModList ResolveModList(this AppFiles files, string name)
    {
        if (files.TryParseModListRef(name, out var reference))
            return reference;
        return files.Mods.Resolve(name);
    }

    public static bool TryParseModListRef(this AppFiles files, Uri uri, [NotNullWhen(true)] out IPRefWithModList? reference)
    {
        reference = null;
        if (uri.Segments.Length < 2)
            return false;
        switch (uri.Host)
        {
            case Constants.UriSyncHost:
                reference = files.Sync.Ref(uri.Segments[1]);
                return true;
            case Constants.UriModListHost:
                reference = files.Mods.Ref(uri.Segments[1]);
                return true;
            default:
                return false;
        }
    }
    
    public static bool TryParseConnectionRef(this AppFiles files, string name, [NotNullWhen(true)]out IPRefWithClientConnection? reference)
    {
        try
        {
            var uri = new Uri(name);
            if (uri.Scheme == Constants.UriScheme)
                return files.TryParseConnectionRef(uri, out reference);
            reference = null;
            return false;
        }
        catch
        {
            reference = null;
            return false;
        }
    }

    public static IPRefWithClientConnection ResolveClientConnectionSource(this AppFiles files, string name)
    {
        if (files.TryParseConnectionRef(name, out var reference))
            return reference;
        return files.Client.Resolve(name);
    }

    public static ClientConnectionRef? ResolveClientConnectionRef(this AppFiles files, string name)
    {
        try
        {
            var uri = new Uri(name);
            if (uri.Segments.Length < 3) return null;
            if (files.TryParseConnectionRef(uri, out var reference))
                return new ClientConnectionRef(reference, uri.Segments[2]);
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    public static bool TryParseConnectionRef(this AppFiles files, Uri uri, [NotNullWhen(true)]out IPRefWithClientConnection? reference)
    {
        reference = null;
        if (uri.Segments.Length < 2) return false;
        switch (uri.Host)
        {
            case Constants.UriSyncHost:
                reference = files.Sync.Ref(uri.Segments[1]);
                return true;
            case Constants.UriClientHost:
                reference = files.Client.Ref(uri.Segments[1]);
                return true;
            default:
                return false;
        }
    }

    public static bool TryParse<T, TRef>(this AppFiles files, string name, [NotNullWhen(true)] out TRef? reference)
        where T : JsonFile<T>
        where TRef : class,IPRef<T, TRef>
    {
        try
        {
            var uri = new Uri(name);
            return files.TryParse<T, TRef>(uri, out reference);
        }
        catch
        {
            reference = null;
            return false;
        }
    }
    
    public static bool TryParse<T, TRef>(this AppFiles files, Uri uri, [NotNullWhen(true)] out TRef? reference)
        where T : JsonFile<T> 
        where TRef : class,IPRef<T, TRef>
    {
        reference = null;
        if (uri.Segments.Length < 2) return false;
        reference = files.GetFileHandler<T, TRef>(uri)?.Ref(uri.Segments[1]);
        return reference != null;
    }

    public static IAppFileHandler<T, TRef>? GetFileHandler<T, TRef>(this AppFiles files, Uri uri)
        where T : JsonFile<T>
        where TRef : class,IPRef<T, TRef>
    {
        try
        {
            if (uri.Host != GetFileHost<T, TRef>()) return null;
            switch (uri.Host)
            {
                case Constants.UriSyncHost:
                    return (IAppFileHandler<T, TRef>)files.Sync;
                case Constants.UriClientHost:
                    return (IAppFileHandler<T, TRef>)files.Client;
                case Constants.UriServerHost:
                    return (IAppFileHandler<T, TRef>)files.Server;
                case Constants.UriModListHost:
                    return (IAppFileHandler<T, TRef>)files.Mods;
                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    public static string? GetFileHost<T, TRef>()
        where T : JsonFile<T>
        where TRef : class,IPRef<T, TRef>
    {
        if (typeof(TRef) == typeof(ClientProfileRef))
            return Constants.UriClientHost;
        if (typeof(TRef) == typeof(ServerProfileRef))
            return Constants.UriServerHost;
        if (typeof(TRef) == typeof(ModListProfileRef))
            return Constants.UriModListHost;
        if (typeof(TRef) == typeof(SyncProfileRef))
            return Constants.UriSyncHost;
        return null;
    }

    public static TRef Resolve<T, TRef>(this IAppFileHandler<T, TRef> handler, string data)
        where T : JsonFile<T>
        where TRef : class, IPRef<T, TRef>
    {
        try
        {
            var uri = new Uri(data);
            if (uri.Segments.Length >= 2 && uri.Host == GetFileHost<T, TRef>())
                return handler.Resolve(handler.Ref(uri.Segments[1]));
            return handler.Resolve(handler.Ref(data));
        }
        catch
        {
            return handler.Resolve(handler.Ref(data));
        }
    }

    public static string GetDirectory<T, TRef>(this IAppFileHandler<T, TRef> handler, TRef reference) 
        where T : JsonFile<T> 
        where TRef : IPRef<T, TRef>
    {
        return Path.GetDirectoryName(handler.GetPath(reference)) ?? throw new DirectoryNotFoundException();
    }

    public static bool TryGet<T, TRef>(this IAppFileHandler<T, TRef> handler, TRef reference, [NotNullWhen(true)] out T? file)
        where T : JsonFile<T>
        where TRef : class,IPRef<T, TRef>
    {
        file = null; 
        if (!handler.Exists(reference)) return false;
        try
        {
            file = handler.Get(reference);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static T Get<T, TRef>(this IPRef<T, TRef> reference)
        where T : JsonFile<T>
        where TRef : class,IPRef<T, TRef>
    {
        return reference.Handler.Get((TRef)reference);
    }

    public static IEnumerable<ClientConnectionRef> GetConnectionRefs(this IPRefWithClientConnection reference)
    {
        return reference.GetConnections().Select(x => new ClientConnectionRef(reference, x.Name));
    }

    public static IEnumerable<ulong> GetModsFromList(this IPRefWithModList reference)
    {
        if (reference.TryGetModList(out var list))
            foreach (var m in list)
                if (ModListUtil.TryParseModId(m, out ulong id))
                    yield return id;
    }
    
    public static IEnumerable<ulong> GetModsFromList(this IEnumerable<IPRefWithModList> references)
    {
        return references.SelectMany(x => x.GetModsFromList());
    }
}