using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.Services;

public class SteamAPI(Steam steam, AppModlistFiles modlistFiles, TaskBlocker taskBlocker)
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
            if (!modlistFiles.ResolveMod(ref mod) && !updated.Contains(pubId))
                updated.Add(pubId);
        } 
        return updated;
    }

    public async Task UpdateMods(List<ulong> list)
    {
        var cts = taskBlocker.Set(Operations.SteamDownload);
        await steam.UpdateMods(list, cts);
        taskBlocker.Release(Operations.SteamDownload);
    }
}