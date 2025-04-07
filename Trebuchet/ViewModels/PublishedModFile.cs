using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using ReactiveUI;

namespace Trebuchet.ViewModels.Panels;

public class PublishedModFile : ReactiveObject, IModFile, IPublishedModFile
{
    public PublishedModFile(string path, ulong publishedId)
    {
        PublishedId = publishedId;
        FilePath = path;
        Title = Path.GetFileName(path);
        LastUpdate = string.Empty;
        IconClasses = "ModIcon Live";
        if (File.Exists(path))
            StatusClasses = "ModStatus Loading";
        else
            StatusClasses = "ModStatus Missing";
    }
    
    public ulong PublishedId { get; }
    public string Title { get; }
    public string StatusClasses { get; }
    public string IconClasses { get; }
    public string LastUpdate { get; }
    public string FilePath { get; }
    public ObservableCollection<ModFileAction> Actions { get; } = [];
    
    public string Export()
    {
        return PublishedId.ToString();
    }
}