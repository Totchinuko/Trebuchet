using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using SteamWorksWebAPI;
using Trebuchet.Assets;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.ViewModels;
using Trebuchet.ViewModels.Panels;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;

namespace Trebuchet.Services;

public class ModFileFactory(AppFiles appFiles, SteamAPI steam, TaskBlocker.TaskBlocker taskBlocker)
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
    
    public IModFile Create(IModFile modfile, string path)
    {
        switch (modfile)
        {
            case LocalModFile:
                return new LocalModFile(path);
            case PublishedModFile pub:
                return new PublishedModFile(path, pub.PublishedId);
            case WorkshopModFile w:
                return new WorkshopModFile(path, w);
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
            if (current is not PublishedModFile pub) continue;
            var workshop = details.FirstOrDefault(d => d.PublishedFileID == pub.PublishedId);
            if (workshop is null) continue;
            files[i] = Create(workshop, needUpdate.Contains(workshop.PublishedFileID));
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
        file.Actions.Add(new ModFileAction(
            Resources.RemoveFromList, 
            "mdi-delete", 
            ReactiveCommand.Create(() => Removed?.Invoke(file)), 
            "Base Red"));
    }

    private void AddOpenWorkshopAction(IPublishedModFile file)
    {
        file.Actions.Add(new ModFileAction(
            Resources.OpenWorkshopPage,
            "mdi-steam",
            ReactiveCommand.Create(() =>
            {
                TrebuchetUtils.Utils.OpenWeb(string.Format(Constants.SteamWorkshopURL, file.PublishedId));
            }))
        );
    }

    private void AddUpdateAction(IPublishedModFile file)
    {
        var canExecute = taskBlocker.WhenAnyValue(x => x.BlockingTypes)
            .Select(x => !x.Intersect([typeof(SteamDownload), typeof(ServersRunning), typeof(ClientRunning)]).Any());
        file.Actions.Add(new ModFileAction(
            Resources.Update,
            "mdi-update",
            ReactiveCommand.Create(() => Updated?.Invoke(file), canExecute)
            ));
    }
    
    private void AddOpenWorkshopDisabledAction(IModFile file)
    {
        var canExecute = taskBlocker.WhenAnyValue(x => x.BlockingTypes).Select(x => false);
        file.Actions.Add(new ModFileAction(
            Resources.Update,
            "mdi-steam",
            ReactiveCommand.Create(() => {}, canExecute)
        ));
    }
    
    private void AddUpdateDisabledAction(IModFile file)
    {
        var canExecute = taskBlocker.WhenAnyValue(x => x.BlockingTypes).Select(x => false);
        file.Actions.Add(new ModFileAction(
            Resources.Update,
            "mdi-update",
            ReactiveCommand.Create(() => {}, canExecute)
        ));
    }
}