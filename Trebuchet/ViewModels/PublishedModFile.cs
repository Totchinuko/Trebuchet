using System.Collections.ObjectModel;
using System.IO;
using ReactiveUI;
using Trebuchet.Assets;

namespace Trebuchet.ViewModels;

public class PublishedModFile : ReactiveObject, IPublishedModFile
{
    public PublishedModFile(string path, ulong publishedId)
    {
        IconClasses.Add(@"ModIcon");
        StatusClasses.Add(@"ModStatus");
        PublishedId = publishedId;
        FilePath = path;
        Title = Path.GetFileName(path);
        LastUpdate = string.Empty;
        IconClasses.Add(@"Live");
        IconToolTip = Resources.LiveMod;
        FileSize = 0;
        StatusClasses.Add(File.Exists(path) ? @"Loading" : @"Missing");
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
    
    public string Export()
    {
        return PublishedId.ToString();
    }
}