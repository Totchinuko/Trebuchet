using System;
using System.IO;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingDirectory : ValidatedInputDialogue<string, OnBoardingDirectory>
{

    public OnBoardingDirectory(string title, string description, string defaultPath = "") : base(title, description)
    {
        SearchDirectoryCommand = ReactiveCommand.Create(OnSearchDirectory);
        Value = defaultPath;
    }

    public ReactiveCommand<Unit, Unit> SearchDirectoryCommand { get; }
    
    private async void OnSearchDirectory()
    {
        var defaultFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new Exception(@"App is installed in an invalid directory");
        if(Application.Current is null || Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new Exception(@"The current application is not a desktop application.");
        
        var toplevel = TopLevel.GetTopLevel(desktop.MainWindow);
        if (toplevel == null) return;
        var folder = await toplevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            SuggestedStartLocation = await toplevel.StorageProvider.TryGetFolderFromPathAsync(defaultFolder)
        });

        if (folder.Count == 0) return;
        if (!folder[0].Path.IsFile) return;
        Value = folder[0].Path.LocalPath;
    }
}