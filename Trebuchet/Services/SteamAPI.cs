using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.Assets;
using TrebuchetLib.Services;

namespace Trebuchet.Services;

public class SteamAPI(Steam steam, AppFiles appFiles, TaskBlocker.TaskBlocker taskBlocker)
{
    private Dictionary<ulong, PublishedFile> _publishedFiles = [];
    
    public async Task<List<PublishedFile>> RequestModDetails(List<ulong> list)
    {
        var results = GetCache(list);
        var response = await SteamRemoteStorage.GetPublishedFileDetails(new GetPublishedFileDetailsQuery(list), CancellationToken.None);
        foreach (var r in response.PublishedFileDetails)
        {
            results.Add(r);
            _publishedFiles[r.PublishedFileID] = r;
        }
        
        return results;
    }

    public void InvalidateCache()
    {
        _publishedFiles.Clear();
    }

    public List<PublishedFile> GetCache(List<ulong> list)
    {
        List<PublishedFile> results = [];
        for (var i = list.Count - 1; i >= 0; i--)
        {
            var mod = list[i];
            if (_publishedFiles.TryGetValue(mod, out var file))
            {
                list.RemoveAt(i);
                results.Add(file);
            }
        }

        return results;
    }
    
    public List<ulong> CheckModsForUpdate(ICollection<(ulong pubId, ulong manifestId)> mods)
    {
        var updated = steam.GetUpdatedUGCFileIDs(mods).ToList();
        foreach (var (pubId, _) in mods)
        {
            string mod = pubId.ToString();
            if (!appFiles.Mods.ResolveMod(ref mod) && !updated.Contains(pubId))
                updated.Add(pubId);
        } 
        return updated;
    }

    public async Task UpdateMods(List<ulong> list)
    {
        var task = await taskBlocker.EnterAsync(new SteamDownload(Resources.UpdateModsLabel));
        try
        {
            await steam.UpdateMods(list, task.Cts);
        }
        catch (OperationCanceledException){}
        finally
        {
            task.Release();
        }
    }

    public async Task UpdateServers()
    {
        var task = await taskBlocker.EnterAsync(new SteamDownload(Resources.UpdateServersLabel));
        try
        {
            await steam.UpdateServerInstances(task.Cts);
        }
        catch (OperationCanceledException){}
        finally
        {
            task.Release();
        }
    }

    public async Task VerifyFiles(IEnumerable<ulong> modlist)
    {
        var task = await taskBlocker.EnterAsync(new SteamDownload(Resources.VerifyServersLabel));
        steam.ClearCache();
        try
        {
            await steam.UpdateServerInstances(task.Cts);
        }
        catch (OperationCanceledException){}
        finally
        {
            task.Release();
        }
        
        task = await taskBlocker.EnterAsync(new SteamDownload(Resources.VerifyModsLabel));
        try
        {
            await steam.UpdateMods(modlist, task.Cts);
        }
        catch (OperationCanceledException) {}
        finally
        {
            task.Release();
        }
    }

    public int GetInstalledServerInstanceCount()
    {
        return steam.GetInstalledInstances();
    }
}