using System.Collections.ObjectModel;
using System.IO;
using Humanizer;
using ReactiveUI;
using Trebuchet.Assets;

namespace Trebuchet.ViewModels;

public class LocalModFile : ReactiveObject, IModFile
{
    public LocalModFile(string path)
    {
        IconClasses.Add(@"ModIcon");
        StatusClasses.Add(@"ModStatus");
        FilePath = path;
        Title = Path.GetFileName(path);
        IconClasses.Add(@"Local");
        IconToolTip = Resources.LocalMod;
        
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists)
        {
            StatusClasses.Add(@"Found");
            FileSize = fileInfo.Length;
            LastUpdate = @$"{Resources.Found} - {Resources.LastModified}: {fileInfo.LastWriteTime.Humanize()} ({FileSize.Bytes().Humanize()})";
        }
        else
        {
            StatusClasses.Add(@"Missing");
            LastUpdate = Resources.Missing;
            FileSize = 0;
        }
    }
    
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
        return FilePath;
    }
}