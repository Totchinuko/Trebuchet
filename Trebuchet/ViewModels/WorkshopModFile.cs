using System;
using System.Collections.ObjectModel;
using System.IO;
using Humanizer;
using ReactiveUI;
using SteamWorksWebAPI;
using Trebuchet.Assets;
using Trebuchet.ViewModels.Panels;
using TrebuchetLib;

namespace Trebuchet.ViewModels;

public class WorkshopModFile : ReactiveObject, IModFile, IPublishedModFile
{
    public WorkshopModFile(string path, PublishedFile file, bool needUpdate = false)
    {
        FilePath = path;
        PublishedId = file.PublishedFileID;
        Title = file.Title;
        AppId = file.ConsumerAppId;
        var updateDate = Tools.UnixTimeStampToDateTime(file.TimeUpdated).ToLocalTime();
        LastUpdate = $"{Resources.LastUpdate}: {updateDate.Humanize()}";
        IconClasses = file.ConsumerAppId == Constants.AppIDTestLiveClient ? "ModIcon TestLive" : "ModIcon Live";
        if(File.Exists(path))
            StatusClasses = needUpdate ? "ModStatus UpdateAvailable" : "ModStatus Up2Date";
        else
            StatusClasses = "ModStatus Missing";
    }
    
    public WorkshopModFile(string path, WorkshopSearchResult file, bool needUpdate = false)
    {
        FilePath = path;
        PublishedId = file.PublishedFileId;
        Title = file.Title;
        AppId = file.AppId;
        LastUpdate = $"{Resources.LastUpdate}: {file.LastUpdate.Humanize()}";
        IconClasses = file.AppId == Constants.AppIDTestLiveClient ? "ModIcon TestLive" : "ModIcon Live";
        if(File.Exists(path))
            StatusClasses = needUpdate ? "ModStatus UpdateAvailable" : "ModStatus Up2Date";
        else
            StatusClasses = "ModStatus Missing";
    }
    
    public WorkshopModFile(string path, WorkshopModFile file)
    {
        FilePath = path;
        PublishedId = file.PublishedId;
        Title = file.Title;
        AppId = file.AppId;
        NeedUpdate = file.NeedUpdate;
        LastUpdate = $"{Resources.LastUpdate}: {file.LastUpdate.Humanize()}";
        IconClasses = file.AppId == Constants.AppIDTestLiveClient ? "ModIcon TestLive" : "ModIcon Live";
        if(File.Exists(path))
            StatusClasses = NeedUpdate ? "ModStatus UpdateAvailable" : "ModStatus Up2Date";
        else
            StatusClasses = "ModStatus Missing";
    }
    
    public bool NeedUpdate { get; }
    public uint AppId { get; }
    public ulong PublishedId { get; }
    public string Title { get; }
    public string FilePath { get; }
    public string StatusClasses { get; }
    public string IconClasses { get; }
    public string LastUpdate { get; }
    public ObservableCollection<ModFileAction> Actions { get; } = [];
    
    public string Export()
    {
        return PublishedId.ToString();
    }
}