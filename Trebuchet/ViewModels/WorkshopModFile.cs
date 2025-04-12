using System;
using System.Collections.ObjectModel;
using System.IO;
using Humanizer;
using ReactiveUI;
using SteamWorksWebAPI;
using Trebuchet.Assets;
using TrebuchetLib;

namespace Trebuchet.ViewModels;

public class WorkshopModFile : ReactiveObject, IPublishedModFile
{
    public WorkshopModFile(string path, PublishedFile file, bool needUpdate = false)
    {
        IconClasses.Add(@"ModIcon");
        StatusClasses.Add(@"ModStatus");
        FilePath = path;
        PublishedId = file.PublishedFileID;
        Title = file.Title;
        AppId = file.ConsumerAppId;
        var updateDate = Tools.UnixTimeStampToDateTime(file.TimeUpdated).ToLocalTime();
        LastDateUpdate = updateDate;
        LastUpdate = @$"{Resources.LastUpdate}: {updateDate.Humanize()}";
        IconClasses.Add(file.ConsumerAppId == Constants.AppIDTestLiveClient ? @"TestLive" : @"Live");
        FileSize = file.FileSize;
        if(File.Exists(path))
            StatusClasses.Add(needUpdate ? @"UpdateAvailable" : @"Up2Date");
        else
        {
            StatusClasses.Add(@"Missing");
            NeedUpdate = true;
        }
    }
    
    public WorkshopModFile(string path, WorkshopSearchResult file, bool needUpdate = false)
    {
        IconClasses.Add(@"ModIcon");
        StatusClasses.Add(@"ModStatus");
        FilePath = path;
        PublishedId = file.PublishedFileId;
        Title = file.Title;
        AppId = file.AppId;
        LastDateUpdate = file.LastUpdate;
        LastUpdate = @$"{Resources.LastUpdate}: {file.LastUpdate.Humanize()}";
        IconClasses.Add(file.AppId == Constants.AppIDTestLiveClient ? @"TestLive" : @"Live");
        FileSize = (long)file.Size;
        if(File.Exists(path))
            StatusClasses.Add(needUpdate ? @"UpdateAvailable" : @"Up2Date");
        else
        {
            StatusClasses.Add(@"Missing");
            NeedUpdate = true;
        }
    }
    
    public WorkshopModFile(string path, WorkshopModFile file)
    {
        IconClasses.Add(@"ModIcon");
        StatusClasses.Add(@"ModStatus");
        FilePath = path;
        PublishedId = file.PublishedId;
        Title = file.Title;
        AppId = file.AppId;
        NeedUpdate = file.NeedUpdate;
        LastDateUpdate = file.LastDateUpdate;
        LastUpdate = @$"{Resources.LastUpdate}: {LastDateUpdate.Humanize()}";
        IconClasses.Add(file.AppId == Constants.AppIDTestLiveClient ? @"TestLive" : @"Live");
        FileSize = file.FileSize;
        if(File.Exists(path))
            StatusClasses.Add(NeedUpdate ? @"UpdateAvailable" : @"Up2Date");
        else
            StatusClasses.Add(@"Missing");
    }
    
    public bool NeedUpdate { get; }
    public uint AppId { get; }
    public ulong PublishedId { get; }
    public string Title { get; }
    public string FilePath { get; }
    public long FileSize { get; }
    public DateTime LastDateUpdate { get; }
    public ObservableCollection<string> StatusClasses { get; } = [];
    public ObservableCollection<string> IconClasses { get; } = [];
    public string LastUpdate { get; }
    public ObservableCollection<ModFileAction> Actions { get; } = [];
    
    public string Export()
    {
        return PublishedId.ToString();
    }
}