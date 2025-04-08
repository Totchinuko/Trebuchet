using System;
using System.Collections.ObjectModel;
using System.IO;
using Humanizer;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.ViewModels.Panels;

namespace Trebuchet.ViewModels;

public class LocalModFile : ReactiveObject, IModFile
{
    public LocalModFile(string path)
    {
        IconClasses.Add("ModIcon");
        StatusClasses.Add("ModStatus");
        FilePath = path;
        Title = Path.GetFileName(path);
        IconClasses.Add("Local");
        
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists)
        {
            StatusClasses.Add("Found");
            LastUpdate = $"{Resources.LastModified}: {fileInfo.LastWriteTime.Humanize()}";
        }
        else
        {
            StatusClasses.Add("Missing");
            LastUpdate = string.Empty;
        }
    }
    
    public string Title { get; }
    public ObservableCollection<string> StatusClasses { get; } = [];
    public ObservableCollection<string> IconClasses { get; } = [];
    public string LastUpdate { get; }
    public string FilePath { get; }
    public ObservableCollection<ModFileAction> Actions { get; } = [];

    public string Export()
    {
        return FilePath;
    }
}