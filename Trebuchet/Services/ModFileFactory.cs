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
    public event IModFileArgs? Removed;
    public event IPublishedModFileArgs? Updated;
    
    public IModFile Create(string mod)
    {
        var path = mod;
        appFiles.Mods.ResolveMod(ref path);
        if (ulong.TryParse(mod, out var publishedFileId))
            return CreatePublished(path, publishedFileId);
        return CreateLocal(path);
    }

    public IModFile CreateUnknown(string path, ulong publishedFile)
    {
        var file = new UnknownModFile(path, publishedFile);
        AddOpenWorkshopDisabledAction(file);
        AddUpdateDisabledAction(file);
        AddRemoveAction(file);
        return file;
    }

    public IModFile Create(IModFile modfile, string path)
    {
        switch (modfile)
        {
            case LocalModFile:
                var file = new LocalModFile(path);
                AddOpenWorkshopDisabledAction(file);
                AddUpdateDisabledAction(file);
                AddRemoveAction(file);
                return file;
            case PublishedModFile pub:
                var pfile =  new PublishedModFile(path, pub.PublishedId);
                AddOpenWorkshopAction(pfile);
                AddUpdateAction(pfile);
                AddRemoveAction(pfile);
                return pfile;
            case WorkshopModFile w:
                var wfile =  new WorkshopModFile(path, w);
                AddOpenWorkshopAction(wfile);
                AddUpdateAction(wfile);
                AddRemoveAction(wfile);
                return wfile;
            case UnknownModFile u:
                var ufile = new UnknownModFile(path, u.PublishedId);
                AddOpenWorkshopDisabledAction(ufile);
                AddUpdateDisabledAction(ufile);
                AddRemoveAction(ufile);
                return ufile;
            default:
                throw new NotImplementedException();
        }
    }

    public async Task QueryFromWorkshop(IList<IModFile> files)
    {
        var published = files.OfType<IPublishedModFile>().Select(x => x.PublishedId).ToList();
        var details = await steam.RequestModDetails(published);
        var needUpdate = steam.CheckModsForUpdate(details.GetManifestKeyValuePairs().ToList());
        for (var i = 0; i < files.Count; i++)
        {
            var current = files[i];
            if (current is not IPublishedModFile pub) continue;
            var workshop = details.FirstOrDefault(d => d.PublishedFileID == pub.PublishedId);
            if (workshop is null) continue;
            if (workshop.CreatorAppId != 0)
                files[i] = Create(workshop, needUpdate.Contains(workshop.PublishedFileID));
            else
                files[i] = CreateUnknown(pub.FilePath, pub.PublishedId);
        }
    }

    public async Task<IModFile> Create(WorkshopSearchResult workshopFile)
    {
        var path = workshopFile.PublishedFileId.ToString();
        appFiles.Mods.ResolveMod(ref path);
        var details = await steam.RequestModDetails([workshopFile.PublishedFileId]);
        var needUpdate = steam.CheckModsForUpdate(details.GetManifestKeyValuePairs().ToList()).Any();
        var file = new WorkshopModFile(path, workshopFile, needUpdate);
        AddOpenWorkshopAction(file);
        AddUpdateAction(file);
        AddRemoveAction(file);
        return file;
    }
    
    public async Task<IModFile> Create(PublishedFile workshopFile)
    {
        var path = workshopFile.PublishedFileID.ToString();
        appFiles.Mods.ResolveMod(ref path);
        var details = await steam.RequestModDetails([workshopFile.PublishedFileID]);
        var needUpdate = steam.CheckModsForUpdate(details.GetManifestKeyValuePairs().ToList()).Any();
        var file = new WorkshopModFile(path, workshopFile, needUpdate);
        AddOpenWorkshopAction(file);
        AddUpdateAction(file);
        AddRemoveAction(file);
        return file;
    }
    
    private IModFile Create(PublishedFile workshopFile, bool needUpdate)
    {
        var path = workshopFile.PublishedFileID.ToString();
        appFiles.Mods.ResolveMod(ref path);
        var file = new WorkshopModFile(path, workshopFile, needUpdate);
        AddOpenWorkshopAction(file);
        AddUpdateAction(file);
        AddRemoveAction(file);
        return file;
    }

    private IModFile CreatePublished(string path, ulong publishedFile)
    {
        var file = new PublishedModFile(path, publishedFile);
        AddOpenWorkshopAction(file);
        AddUpdateAction(file);
        AddRemoveAction(file);
        return file;
    }

    private IModFile CreateLocal(string path)
    {
        var file = new LocalModFile(path);
        AddOpenWorkshopDisabledAction(file);
        AddUpdateDisabledAction(file);
        AddRemoveAction(file);
        return file;
    }
    
    private void AddRemoveAction(IModFile file)
    {
        var action = new ModFileAction(
            Resources.RemoveFromList,
            "mdi-delete",
            ReactiveCommand.Create(() => Removed?.Invoke(file)));
        action.Classes.Add(@"Red");
        file.Actions.Add(action);
    }

    private void AddOpenWorkshopAction(IPublishedModFile file)
    {
        file.Actions.Add(new ModFileAction(
            Resources.OpenWorkshopPage,
            "mdi-steam",
            ReactiveCommand.Create(() =>
            {
                tot_lib.Utils.OpenWeb(string.Format(Constants.SteamWorkshopURL, file.PublishedId));
            }))
        );
    }

    private void AddUpdateAction(IPublishedModFile file)
    {
        var canExecute = taskBlocker.WhenAnyValue(x => x.CanDownloadMods);
        file.Actions.Add(new ModFileAction(
            Resources.Update,
            "mdi-update",
            ReactiveCommand.Create(() => Updated?.Invoke(file), canExecute)
            ));
    }
    
    private void AddOpenWorkshopDisabledAction(IModFile file)
    {
        file.Actions.Add(new ModFileAction(
            Resources.Update,
            "mdi-steam",
            ReactiveCommand.Create(() => {}, Observable.Empty<bool>().StartWith(false))
        ));
    }
    
    private void AddUpdateDisabledAction(IModFile file)
    {
        file.Actions.Add(new ModFileAction(
            Resources.Update,
            "mdi-update",
            ReactiveCommand.Create(() => {}, Observable.Empty<bool>().StartWith(false))
        ));
    }
}