using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using Trebuchet.Assets;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingModlistImport : ValidatedInputDialogue<string, OnBoardingModlistImport>
{
    public OnBoardingModlistImport(string text, bool export, FilePickerFileType type) : base(Resources.ImportModList, string.Empty)
    {
        SetStretch<OnBoardingModlistImport>();
        Value = text;
        _export = export;
        _fileType = type;
        AppendCommand = ReactiveCommand.Create(OnAppend);
        ApplyCommand = ReactiveCommand.Create(OnApply);
        SaveCommand = ReactiveCommand.CreateFromTask(OnSave);
    }
    
    private bool _append;
    private bool _canceled = true;
    private readonly bool _export;
    private readonly FilePickerFileType _fileType;
    
    public ReactiveCommand<Unit, Unit> AppendCommand { get; }
    public ReactiveCommand<Unit, Unit> ApplyCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public bool Append { get => _append; set => _append = value; }
    
    public bool Canceled { get => _canceled; set => _canceled = value; }
    public bool ImportVisible => !_export;
    public bool SaveAsVisible => _export;
    
    private void OnAppend()
    {
        if (_export) return;
        _append = true;
        _canceled = false;
        Close();
    }

    private void OnApply()
    {
        if (_export) return;
        _canceled = false;
        Close();
    }

    private async Task OnSave()
    {
        if (!_export) return;
        if (string.IsNullOrEmpty(Value)) return;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        if (desktop.MainWindow == null) return;

        var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = Resources.SaveFile,
            SuggestedFileName = Resources.Untitled,
            FileTypeChoices = [_fileType]
        });

        if (file is null) return;
        if (!file.Path.IsFile) return;
        var path = Path.GetFullPath(file.Path.LocalPath);
        await File.WriteAllTextAsync(path, Value);
        Close();
    }
}