using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using SteamWorksWebAPI;
using Trebuchet.Assets;
using Trebuchet.Utils;
using Trebuchet.ViewModels;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.Services;

public class ModFileFactory(AppFiles appFiles, SteamApi steam, TaskBlocker.TaskBlocker taskBlocker)
{
    public ModFileBuilder Create(string mod)
    {
        var path = mod;
        appFiles.Mods.ResolveMod(ref path);
        if (ulong.TryParse(mod, out var publishedFileId))
            return CreatePublished(path, publishedFileId);
        return CreateLocal(path);
    }

    public ModFileBuilder CreateUnknown(string path, ulong publishedFile)
    {
        var file = new UnknownModFile(path, publishedFile);
        return new ModFileBuilder(file, taskBlocker);
    }

    public ModFileBuilder Create(IModFile modfile, string path)
    {
        switch (modfile)
        {
            case LocalModFile:
                var file = new LocalModFile(path);
                return new ModFileBuilder(file, taskBlocker);
            case PublishedModFile pub:
                var pfile =  new PublishedModFile(path, pub.PublishedId);
                return new ModFileBuilder(pfile, taskBlocker);
            case WorkshopModFile w:
                var wfile =  new WorkshopModFile(path, w);
                return new ModFileBuilder(wfile, taskBlocker);
            case UnknownModFile u:
                var ufile = new UnknownModFile(path, u.PublishedId);
                return new ModFileBuilder(ufile, taskBlocker);
            default:
                throw new NotImplementedException();
        }
    }

    public async Task<ModFileBuilder> Create(WorkshopSearchResult workshopFile)
    {
        var path = workshopFile.PublishedFileId.ToString();
        appFiles.Mods.ResolveMod(ref path);
        var details = await steam.RequestModDetails([workshopFile.PublishedFileId]);
        var status = steam.CheckModsForUpdate(details.GetManifestKeyValuePairs().ToList())
            .FirstOrDefault(UGCFileStatus.Default(workshopFile.PublishedFileId));
        var file = new WorkshopModFile(path, workshopFile, status);
        return new ModFileBuilder(file, taskBlocker);
    }
    
    public async Task<ModFileBuilder> Create(PublishedFile workshopFile)
    {
        var path = workshopFile.PublishedFileID.ToString();
        appFiles.Mods.ResolveMod(ref path);
        var details = await steam.RequestModDetails([workshopFile.PublishedFileID]);
        var status = steam.CheckModsForUpdate(details.GetManifestKeyValuePairs().ToList())
            .FirstOrDefault(UGCFileStatus.Default(workshopFile.PublishedFileID));
        var file = new WorkshopModFile(path, workshopFile, status);
        return new ModFileBuilder(file, taskBlocker);
    }
    
    public ModFileBuilder Create(PublishedFile workshopFile, UGCFileStatus status)
    {
        var path = workshopFile.PublishedFileID.ToString();
        appFiles.Mods.ResolveMod(ref path);
        var file = new WorkshopModFile(path, workshopFile, status);
        return new ModFileBuilder(file, taskBlocker);
    }

    private ModFileBuilder CreatePublished(string path, ulong publishedFile)
    {
        var file = new PublishedModFile(path, publishedFile);
        return new ModFileBuilder(file, taskBlocker);
    }

    private ModFileBuilder CreateLocal(string path)
    {
        var file = new LocalModFile(path);
        return new ModFileBuilder(file, taskBlocker);
    }
}