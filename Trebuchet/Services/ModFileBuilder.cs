using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvaloniaEdit.Utils;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.ViewModels;
using TrebuchetLib;

namespace Trebuchet.Services;

public class ModFileBuilder(IModFile modFile, TaskBlocker blocker)
{
    public ModFileBuilder SetActions(Func<IModFile, Task> remove, Func<IPublishedModFile, Task>? updater = null)
    {
        SetOpenWorkshopAction();
        SetUpdater(updater);
        SetRemove(remove);
        return this;
    }

    public ModFileBuilder SetActions(Func<IPublishedModFile, Task>? updater = null)
    {
        SetOpenWorkshopAction();
        SetUpdater(updater);
        SetRemoveReadOnly();
        return this; 
    }

    public IModFile Build()
    {
        return modFile;
    }

    public ModFileBuilder SetUpdater(Func<IPublishedModFile, Task>? updater)
    {
        if (modFile is not IPublishedModFile published || updater is null)
        {
            modFile.Actions.Add(new ModFileAction(
                Resources.Update,
                "mdi-update",
                ReactiveCommand.Create(() => {}, Observable.Empty<bool>().StartWith(false))
            ));
            return this;
        }
        
        var canExecute = blocker.WhenAnyValue(x => x.CanDownloadMods);
        modFile.Actions.Add(new ModFileAction(
            Resources.Update,
            "mdi-update",
            ReactiveCommand.CreateFromTask(() => updater.Invoke(published), canExecute)
        ));
        return this;
    }
    
    public ModFileBuilder SetRemove(Func<IModFile, Task> remove)
    {
        var action = new ModFileAction(
            Resources.RemoveFromList,
            "mdi-delete",
            ReactiveCommand.CreateFromTask(() => remove.Invoke(modFile)));
        action.Classes.Add(@"Red");
        modFile.Actions.Add(action);
        return this;
    }

    public ModFileBuilder SetRemoveReadOnly()
    {
        modFile.Actions.Add(new ModFileAction(
            Resources.Update,
            "mdi-delete",
            ReactiveCommand.Create(() => {}, Observable.Empty<bool>().StartWith(false))
        ));
        return this;
    }

    public ModFileBuilder SetOpenWorkshopAction()
    {
        if (modFile is not IPublishedModFile published)
        {
            modFile.Actions.Add(new ModFileAction(
                Resources.Update,
                "mdi-steam",
                ReactiveCommand.Create(() => {}, Observable.Empty<bool>().StartWith(false))
            ));
            return this;
        }
        
        published.Actions.Add(new ModFileAction(
            Resources.OpenWorkshopPage,
            "mdi-steam",
            ReactiveCommand.Create(() =>
            {
                tot_lib.Utils.OpenWeb(string.Format(Constants.SteamWorkshopURL, published.PublishedId));
            }))
        );
        return this;
    }
}