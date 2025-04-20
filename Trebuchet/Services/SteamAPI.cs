using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.Assets;
using Trebuchet.ViewModels;
using TrebuchetLib.Services;

namespace Trebuchet.Services;

public class SteamApi(
    Steam steam, 
    AppFiles appFiles, 
    ILogger<SteamApi> logger,
    TaskBlocker.TaskBlocker taskBlocker)
{
    private readonly Dictionary<ulong, PublishedFile> _publishedFiles = [];
    private DateTime _lastCacheClear = DateTime.MinValue;
    
    public async Task<List<PublishedFile>> RequestModDetails(List<ulong> list)
    {
        var results = GetCache(list);
        if (list.Count <= 0) return results;
        using(logger.BeginScope((@"ModList", list)))
            logger.LogInformation(@"Seeking mod details");
        try
        {
            var response = await SteamRemoteStorage.GetPublishedFileDetails(new GetPublishedFileDetailsQuery(list),
                CancellationToken.None);
            foreach (var r in response.PublishedFileDetails)
            {
                results.Add(r);
                _publishedFiles[r.PublishedFileID] = r;
            }

            return results;
        }
        catch (OperationCanceledException)
        {
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, @"Could not download mod infos");
        }

        return [];
    }

    public void InvalidateCache()
    {
        logger.LogInformation(@"Invalidating mod details cache");
        _publishedFiles.Clear();
        _lastCacheClear = DateTime.UtcNow;
    }

    public List<PublishedFile> GetCache(List<ulong> list)
    {
        List<PublishedFile> results = [];
        if ((DateTime.UtcNow - _lastCacheClear).TotalMinutes > 1.0)
            InvalidateCache();
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
        using(logger.BeginScope((@"ModList", list)))
            logger.LogInformation(@"Updating mods");
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
        logger.LogInformation(@"Updating servers");
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
        logger.LogInformation(@"Verifying server files");
        steam.ClearCache();
        InvalidateCache();
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
        var enumerable = modlist.ToList();
        using(logger.BeginScope((@"ModList", enumerable)))
            logger.LogInformation(@"Verifying mod files");
        try
        {
            await steam.UpdateMods(enumerable, task.Cts);
        }
        catch (OperationCanceledException) {}
        finally
        {
            task.Release();
        }
    }

    public int CountUnusedMods()
    {
        var installedMods = steam.GetUGCFileIdsFromStorage();
        var usedMods = appFiles.Mods.ListProfiles()
            .SelectMany(x => appFiles.Mods.Get(x).GetWorkshopMods());
        var count = installedMods.Except(usedMods).Count();
        return count;
    }

    public async Task RemoveUnusedMods()
    {
        var task = await taskBlocker.EnterAsync(new SteamDownload(Resources.TrimmingUnusedMods));
        try
        {
            var installedMods = steam.GetUGCFileIdsFromStorage();
            var usedMods = appFiles.Mods.ListProfiles()
                .SelectMany(x => appFiles.Mods.Get(x).GetWorkshopMods());
            var toRemove = installedMods.Except(usedMods).ToList();
            steam.ClearUGCFileIdsFromStorage(toRemove);

            logger.LogInformation(@"Cleaning unused mods");
            foreach (var mod in toRemove)
            {
                var path = mod.ToString();
                if (!appFiles.Mods.ResolveMod(ref path)) continue;
                if (!File.Exists(path)) continue;
                logger.LogInformation(path);
                File.Delete(path);
            }
        }
        catch (OperationCanceledException){}
        finally
        {
            task.Release();
        }
    }


    public IDisposable SetDownloaderProgress(IProgress<double> progress)
    {
        steam.SetTemporaryProgress(progress);
        return new SteamProgressRestore(steam);
    }

    public int GetInstalledServerInstanceCount()
    {
        return steam.GetInstalledInstances();
    }
}