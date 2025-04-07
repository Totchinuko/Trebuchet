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
        FilePath = path;
        Title = Path.GetFileName(path);
        IconClasses = "ModIcon Local";
        
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists)
        {
            StatusClasses = "ModStatus Found";
            LastUpdate = $"{Resources.LastModified}: {fileInfo.LastWriteTime.Humanize()}";
        }
        else
        {
            StatusClasses = "ModStatus Missing";
            LastUpdate = string.Empty;
        }
    }
    
    public string Title { get; }
    public string StatusClasses { get; }
    public string IconClasses { get; }
    public string LastUpdate { get; }
    public string FilePath { get; }
    public ObservableCollection<ModFileAction> Actions { get; } = [];

    public string Export()
    {
        return FilePath;
    }
}