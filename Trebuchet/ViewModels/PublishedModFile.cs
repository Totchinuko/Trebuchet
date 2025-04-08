using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using ReactiveUI;

namespace Trebuchet.ViewModels.Panels;

public class PublishedModFile : ReactiveObject, IModFile, IPublishedModFile
{
    public PublishedModFile(string path, ulong publishedId)
    {
        IconClasses.Add("ModIcon");
        StatusClasses.Add("ModStatus");
        PublishedId = publishedId;
        FilePath = path;
        Title = Path.GetFileName(path);
        LastUpdate = string.Empty;
        IconClasses.Add("Live");
        if (File.Exists(path))
            StatusClasses.Add("Loading");
        else
            StatusClasses.Add("Missing");
    }
    
    public ulong PublishedId { get; }
    public string Title { get; }
    public ObservableCollection<string> StatusClasses { get; } = [];
    public ObservableCollection<string> IconClasses { get; } = [];
    public string LastUpdate { get; }
    public string FilePath { get; }
    public ObservableCollection<ModFileAction> Actions { get; } = [];
    
    public string Export()
    {
        return PublishedId.ToString();
    }
}