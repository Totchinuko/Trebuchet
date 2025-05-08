using System.Collections.ObjectModel;
using System.IO;
using Humanizer;
using ReactiveUI;
using Trebuchet.Assets;

namespace Trebuchet.ViewModels;

public class UnknownModFile : ReactiveObject, IPublishedModFile
{
    public UnknownModFile(string path, ulong publishedId)
    {
        IconClasses.Add(@"ModIcon");
        StatusClasses.Add(@"ModStatus");
        PublishedId = publishedId;
        FilePath = path;
        Title = Path.GetFileName(path);
        IconClasses.Add(@"Unknown");
        IconToolTip = Resources.UnknownMod;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            var fileInfo = new FileInfo(path);
            StatusClasses.Add(@"Found");
            LastUpdate = @$"{Resources.Found} - {Resources.LastModified}: {fileInfo.LastWriteTime.Humanize()} ({FileSize.Bytes().Humanize()})";
            FileSize = fileInfo.Length;
        }
        else
        {
            StatusClasses.Add(@"Missing");
            LastUpdate = Resources.Missing;
            FileSize = 0;
        }
    }
    
    public ulong PublishedId { get; }
    public string Title { get; }
    public ObservableCollection<string> StatusClasses { get; } = [];
    public ObservableCollection<string> IconClasses { get; } = [];
    public string IconToolTip { get; }
    public string LastUpdate { get; }
    public string FilePath { get; }
    public long FileSize { get; }
    public ObservableCollection<ModFileAction> Actions { get; } = [];
    public ModProgressViewModel Progress { get; } = new();
    public string Export()
    {
        return PublishedId.ToString();
    }
}