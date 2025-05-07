using System;
using System.Linq;
using System.Threading.Tasks;
using Trebuchet.ViewModels;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.Services;

public class ModFileFactory(AppSetup setup, Steam steam, TaskBlocker.TaskBlocker taskBlocker)
{
    public ModFileBuilder Create(string mod)
    {
        if (ulong.TryParse(mod, out var publishedFileId))
            return CreatePublished(publishedFileId);
        return CreateLocal(mod);
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
                var pfile =  new PublishedModFile(pub.PublishedId, path);
                return new ModFileBuilder(pfile, taskBlocker);
            case WorkshopModFile w:
                var wfile =  new WorkshopModFile(w, path);
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
        var status = (await steam.RequestModDetails([workshopFile.PublishedFileId]))
            .Select(x => x.Status)
            .FirstOrDefault(UGCFileStatus.Default(workshopFile.PublishedFileId));
        var file = setup.TryGetModPath(workshopFile.PublishedFileId.ToString(), out var path) 
            ? new WorkshopModFile(workshopFile, status, path)
            : new WorkshopModFile(workshopFile, status);
        return new ModFileBuilder(file, taskBlocker);
    }
    
    public ModFileBuilder Create(PublishedMod workshop, UGCFileStatus status)
    {
        var file = setup.TryGetModPath(workshop.PublishedFileId.ToString(), out var path) 
            ? new WorkshopModFile(workshop, status, path)
            : new WorkshopModFile(workshop, status);
        return new ModFileBuilder(file, taskBlocker);
    }

    private ModFileBuilder CreatePublished(ulong publishedFile)
    {
        var file = setup.TryGetModPath(publishedFile.ToString(), out var path) 
            ? new PublishedModFile(publishedFile, path)
            : new PublishedModFile(publishedFile);
        return new ModFileBuilder(file, taskBlocker);
    }

    private ModFileBuilder CreateLocal(string path)
    {
        var file = new LocalModFile(path);
        return new ModFileBuilder(file, taskBlocker);
    }
}