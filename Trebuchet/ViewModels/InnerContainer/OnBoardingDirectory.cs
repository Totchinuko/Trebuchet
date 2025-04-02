using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using TrebuchetUtils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingDirectory : ValidatedInputDialogue<string>
{

    public OnBoardingDirectory(string title, string description) : base(title, description)
    {
        SearchDirectoryCommand = new SimpleCommand().Subscribe(OnSearchDirectory);
    }

    public SimpleCommand SearchDirectoryCommand { get; }
    
    private async void OnSearchDirectory()
    {
        var defaultFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new Exception("App is installed in an invalid directory");
        if(Application.Current is null || Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new Exception("The current application is not a desktop application.");
        
        var toplevel = TopLevel.GetTopLevel(desktop.MainWindow);
        if (toplevel == null) return;
        var folder = await toplevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            SuggestedStartLocation = await toplevel.StorageProvider.TryGetFolderFromPathAsync(defaultFolder)
        });

        if (folder.Count == 0) return;
        if (!folder[0].Path.IsFile) return;
        var result = _validation(folder[0].Path.LocalPath);
        IsValid = result.IsValid;
        ErrorMessage = result.ErrorMessage;
    }
}