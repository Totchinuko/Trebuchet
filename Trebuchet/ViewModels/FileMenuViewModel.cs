using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using tot_lib;
using Trebuchet.Assets;
using Trebuchet.Utils;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels;

public interface IFileMenuViewModel : IReactiveObject
{
    public string Name { get; }
    public bool Exportable { get; }
    public bool IsLoading { get; }
    public IEnumerable<IFileViewModel> List { get; }
    ReactiveCommand<Unit,Unit> Import { get; }
    ReactiveCommand<Unit,Unit> Create { get; }
}

public class FileMenuViewModel<T, TRef> : ReactiveObject, IFileMenuViewModel 
    where T : ProfileFile<T>
    where TRef : class,IPRef<T, TRef>
{
    public string Name { get; }

    public FileMenuViewModel(string name, IAppFileHandler<T, TRef> fileHandler, DialogueBox dialogue, ILogger logger)
    {
        Name = name;
        _fileHandler = fileHandler;
        _dialogue = dialogue;
        _logger = logger;
        _selected = fileHandler.GetDefault();
        Exportable = !fileHandler.UseSubFolders; // todo: When sub folder export is supported, remove

        RefreshList();
        SetupFileWatcher(fileHandler.GetBaseFolder());

        Create = ReactiveCommand.CreateFromTask(OnCreate);
        Import = ReactiveCommand.CreateFromTask(OnImport);

         this.WhenAnyValue(x => x.Selected)
             .InvokeCommand(ReactiveCommand.CreateFromTask<TRef>(OnSelect));
    }
    private FileSystemWatcher _modWatcher;
    private readonly IAppFileHandler<T, TRef> _fileHandler;
    private readonly DialogueBox _dialogue;
    private readonly ILogger _logger;
    private TRef _selected;
    private bool _isLoading;

    public event AsyncEventHandler<TRef>? FileSelected; 
    
    public bool Exportable { get; }
    
    public ReactiveCommand<Unit,Unit> Import { get; }
    public ReactiveCommand<Unit,Unit> Create { get; }

    public ObservableCollectionExtended<FileViewModel<T, TRef>> List { get; } = [];

    IEnumerable<IFileViewModel> IFileMenuViewModel.List => List;

    public TRef Selected
    {
        get => _selected;
        set => this.RaiseAndSetIfChanged(ref _selected, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private void RefreshList()
    {
        using (List.SuspendNotifications())
        {
            List.Clear();
            foreach (var file in _fileHandler.GetList())
            {
                var vm = new FileViewModel<T, TRef>(file, file.Equals(Selected), Exportable);
                vm.Clicked += OnFileClicked;
                List.Add(vm);
            }
        }
    }

    private async Task OnSelect(TRef selected)
    {
        foreach (var file in List)
            file.Selected = file.Reference.Equals(selected);
        
        await OnFileSelected(Selected);
    }

    private async Task OnCreate()
    {
        var name = await GetNewProfileName();
        if (name is null) return;
        var reference = _fileHandler.Ref(name);
        _logger.LogInformation(@"Create {name}", name);
        _fileHandler.Create(reference);
        Selected = reference;
    }

    private async Task OnImport()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        if (desktop.MainWindow == null) return;

        var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = Resources.ImportTxt,
            FileTypeFilter = [FileType.Json],
        });

        if (files.Count <= 0) return;
        if (!files[0].Path.IsFile) return;
        var path = Path.GetFullPath(files[0].Path.LocalPath);

        var name = await GetNewProfileName();
        if (name is null) return;
        var reference = _fileHandler.Ref(name);

        try
        {
            _logger.LogInformation(@"Importing {file} into {name}", path, name);
            await _fileHandler.Import(new FileInfo(path), reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, @"Failed to import");
            await _dialogue.OpenErrorAsync($@"Failed to import the file ({ex.Message})");
        }
    }

    private async Task OnFileClicked(object? sender, FileViewModelEventArgs<T, TRef> args)
    {
        switch (args.Action)
        {
            case FileViewAction.Select:
                Selected = args.Reference;
                break;
            case FileViewAction.Delete:
                if (await Confirmation(Resources.Deletion, string.Format(Resources.DeletionText, args.Reference)))
                {
                    IsLoading = true;
                    _logger.LogInformation(@"Delete {name}", args.Reference);
                    await CatchError(@"delete", () => _fileHandler.Delete(args.Reference));
                    Selected = _fileHandler.GetDefault();
                    IsLoading = false;
                }
                break;
            case FileViewAction.Duplicate:
                var name = await GetNewProfileName();
                if (name is null) return;
                var reference = _fileHandler.Ref(name);
                IsLoading = true;
                _logger.LogInformation(@"Duplicating {name}", name);
                await CatchError(@"duplicate", () => _fileHandler.Duplicate(args.Reference, reference));
                Selected = reference;
                IsLoading = false;
                break;
            case FileViewAction.Export:
                await ExportFile(args.Reference);
                break;
            case FileViewAction.Rename:
                var renamed = await GetNewProfileName(args.Reference.Name);
                if (renamed is null) return;
                var renamedReference = _fileHandler.Ref(renamed);
                _logger.LogInformation(@"Renaming {old} into {new}", args.Reference, renamed);
                IsLoading = true;
                await CatchError(@"rename", () => _fileHandler.Rename(args.Reference, renamedReference));
                Selected = renamedReference;
                IsLoading = false;
                break;
            case FileViewAction.OpenFolder:
                _logger.LogInformation(@"Opening {name} folder", args.Reference);
                string dir = _fileHandler.GetDirectory(args.Reference);
                Process.Start("explorer.exe", dir);
                break;
        }
    }

    private async Task CatchError(string action, Func<Task> task)
    {
        try
        {
            await task.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, @$"Failed to {action}");
            await _dialogue.OpenErrorAsync(@$"Failed to {action} the file ({ex.Message})");
        }
    }
    
    private async Task CatchError(string action, Action task)
    {
        await CatchError(action, () =>
        {
            task.Invoke();
            return Task.CompletedTask;
        });
    }

    private async Task ExportFile(TRef reference)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        if (desktop.MainWindow == null) return;

        var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = Resources.SaveFile,
            SuggestedFileName = reference.Name,
            FileTypeChoices = [FileType.Json]
        });

        if (file is null) return;
        if (!file.Path.IsFile) return;
        var path = Path.GetFullPath(file.Path.LocalPath);

        try
        {
            _logger.LogInformation(@"Exporting {name} into {file}", reference.Name, path);
            await _fileHandler.Export(reference, new FileInfo(path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, @"Failed to export");
            await _dialogue.OpenErrorAsync(@$"Failed to export the file ({ex.Message})");
        }
    }
    
    private Validation ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Validation.Invalid(Resources.ErrorNameEmpty);
        
        name = name.Trim().ToLower();
        if (List.Select(x => x.Name.ToLower()).Contains(name))
            return Validation.Invalid(Resources.ErrorNameAlreadyTaken);
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return Validation.Invalid(Resources.ErrorNameInvalidCharacters);
        return Validation.Valid;
    }
        
    private async Task<string?> GetNewProfileName(string name = "")
    {
        var modal = new OnBoardingNameSelection(Resources.Create, string.Empty)
            .SetValidation(ValidateName);
        modal.Value = name;
        await _dialogue.OpenAsync(modal);
        return modal.Value;
    }

    private async Task<bool> Confirmation(string title, string description)
    {
        OnBoardingConfirmation confirm = new OnBoardingConfirmation(
            title,
            description);
        await _dialogue.OpenAsync(confirm);
        return confirm.Result;
    }

    [MemberNotNull("_modWatcher")]
    private void SetupFileWatcher(string path)
    {
        if (_modWatcher != null)
            return;

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException();

        _modWatcher = new FileSystemWatcher(path);
        _modWatcher.NotifyFilter = NotifyFilters.Attributes
                                   | NotifyFilters.CreationTime
                                   | NotifyFilters.DirectoryName
                                   | NotifyFilters.FileName
                                   | NotifyFilters.LastAccess
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.Security
                                   | NotifyFilters.Size;
        //_modWatcher.Changed += OnFileChanged;
        _modWatcher.Created += OnFileChanged;
        _modWatcher.Deleted += OnFileChanged;
        _modWatcher.Renamed += OnFileChanged;
        _modWatcher.IncludeSubdirectories = false;
        _modWatcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Dispatcher.UIThread.Invoke(RefreshList);
    }

    protected virtual async Task OnFileSelected(TRef args)
    {
        if (FileSelected is not null)
            await FileSelected.Invoke(this, args);
    }
}