using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using Trebuchet.Assets;
using Trebuchet.Utils;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingModlistImport : ValidatedInputDialogue<string, OnBoardingModlistImport>
{
    public OnBoardingModlistImport(string text) : base(Resources.Edit, string.Empty)
    {
        SetStretch<OnBoardingModlistImport>();
        Value = text;
        SaveAsFile = ReactiveCommand.CreateFromTask(OnSaveAsFile);
        OpenFile = ReactiveCommand.CreateFromTask(OnOpenFile);
        SaveModlist = ReactiveCommand.Create(Close);
    }
    
    public ReactiveCommand<Unit, Unit> SaveAsFile { get; }
    public ReactiveCommand<Unit, Unit> OpenFile { get; }
    public ReactiveCommand<Unit, Unit> SaveModlist { get; }
    private async Task OnSaveAsFile()
    {
        if (string.IsNullOrEmpty(Value)) return;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        if (desktop.MainWindow == null) return;

        var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = Resources.SaveFile,
            SuggestedFileName = Resources.Untitled,
            FileTypeChoices = [FileType.Txt]
        });

        if (file is null) return;
        if (!file.Path.IsFile) return;
        var path = Path.GetFullPath(file.Path.LocalPath);
        await File.WriteAllTextAsync(path, Value);
    }
    
    private async Task OnOpenFile()
    {
        if (string.IsNullOrEmpty(Value)) return;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        if (desktop.MainWindow == null) return;

        var file = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = Resources.SaveFile,
            SuggestedFileName = Resources.Untitled,
            AllowMultiple = false,
            FileTypeFilter = [FileType.Txt],
        });

        if (file.Count == 0) return;
        if (!file[0].Path.IsFile) return;
        var path = Path.GetFullPath(file[0].Path.LocalPath);
        Value = await File.ReadAllTextAsync(path);
    }
}