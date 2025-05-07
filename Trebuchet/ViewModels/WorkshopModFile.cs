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
    public WorkshopModFile(PublishedMod file, UGCFileStatus status, string? path = null)
    {
        IconClasses.Add(@"ModIcon");
        StatusClasses.Add(@"ModStatus");
        FilePath = path ?? string.Empty;
        PublishedId = file.PublishedFileId;
        Title = file.Title;
        AppId = file.ConsumerAppId;
        var updateDate = Tools.UnixTimeStampToDateTime(file.TimeUpdated).ToLocalTime();
        LastDateUpdate = updateDate;
        IconClasses.Add(file.ConsumerAppId == Constants.AppIDTestLiveClient ? @"TestLive" : @"Live");
        IconToolTip = file.ConsumerAppId == Constants.AppIDTestLiveClient ? Resources.TestLiveMod : Resources.LiveMod;
        FileSize = file.FileSize;
        Status = status;
        GetStatusElements(out var label, out var xamlClass);
        LastUpdate = label;
        StatusClasses.Add(xamlClass);
    }
    
    public WorkshopModFile(WorkshopSearchResult file, UGCFileStatus status, string? path = null)
    {
        IconClasses.Add(@"ModIcon");
        StatusClasses.Add(@"ModStatus");
        FilePath = path ?? string.Empty;
        PublishedId = file.PublishedFileId;
        Title = file.Title;
        AppId = file.AppId;
        LastDateUpdate = file.LastUpdate;
        IconClasses.Add(file.AppId == Constants.AppIDTestLiveClient ? @"TestLive" : @"Live");
        IconToolTip = file.AppId == Constants.AppIDTestLiveClient ? Resources.TestLiveMod : Resources.LiveMod;
        FileSize = (long)file.Size;
        Status = status;
        GetStatusElements(out var label, out var xamlClass);
        LastUpdate = label;
        StatusClasses.Add(xamlClass);
    }
    
    public WorkshopModFile(WorkshopModFile file, string? path = null)
    {
        IconClasses.Add(@"ModIcon");
        StatusClasses.Add(@"ModStatus");
        FilePath = path ?? string.Empty;
        PublishedId = file.PublishedId;
        Title = file.Title;
        AppId = file.AppId;
        Status = file.Status;
        LastDateUpdate = file.LastDateUpdate;
        IconClasses.Add(file.AppId == Constants.AppIDTestLiveClient ? @"TestLive" : @"Live");
        IconToolTip = file.AppId == Constants.AppIDTestLiveClient ? Resources.TestLiveMod : Resources.LiveMod;
        FileSize = file.FileSize;
        GetStatusElements(out var label, out var xamlClass);
        LastUpdate = label;
        StatusClasses.Add(xamlClass);
    }
    
    public UGCFileStatus Status { get; }
    public uint AppId { get; }
    public ulong PublishedId { get; }
    public string Title { get; }
    public string FilePath { get; }
    public long FileSize { get; }
    public DateTime LastDateUpdate { get; }
    public ObservableCollection<string> StatusClasses { get; } = [];
    public ObservableCollection<string> IconClasses { get; } = [];
    public string IconToolTip { get; }
    public string LastUpdate { get; }
    public ObservableCollection<ModFileAction> Actions { get; } = [];
    public ModProgressViewModel Progress { get; } = new();
    public string Export()
    {
        return PublishedId.ToString();
    }

    private void GetStatusElements(out string label, out string xamlClass)
    {
        if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
        {
            label = @$"{Resources.Missing} - {Resources.LastUpdate}: {LastDateUpdate.Humanize()}";
            xamlClass = @"Missing";
            return;
        }

        switch (Status.Status)
        {
            case UGCStatus.Corrupted:
                label = @$"{Resources.Corrupted} - {Resources.LastUpdate}: {LastDateUpdate.Humanize()} ({FileSize.Bytes().Humanize()})";
                xamlClass = @"Missing";
                break;
            case UGCStatus.Updatable:
                label = @$"{Resources.UpdateAvailable} - {Resources.LastUpdate}: {LastDateUpdate.Humanize()} ({FileSize.Bytes().Humanize()})";
                xamlClass = @"UpdateAvailable";
                break;
            case UGCStatus.UpToDate:
                label = @$"{Resources.UpToDate} - {Resources.LastUpdate}: {LastDateUpdate.Humanize()} ({FileSize.Bytes().Humanize()})";
                xamlClass = @"Up2Date";
                break;
            default:
                label = @$"{Resources.Missing} - {Resources.LastUpdate}: {LastDateUpdate.Humanize()} ({FileSize.Bytes().Humanize()})";
                xamlClass = @"Missing";
                break;
        }
    } 
}