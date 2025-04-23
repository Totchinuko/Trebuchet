using System;
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

public interface IFileMenuViewModel
{
    public string Name { get; }
    public string Selected { get; set; }
    public bool Exportable { get; }
    public bool IsLoading { get; }
    ObservableCollectionExtended<FileViewModel> List { get; }
    ReactiveCommand<Unit,Unit> Import { get; }
    ReactiveCommand<Unit,Unit> Create { get; }

    event AsyncEventHandler<string>? FileSelected;
}

public class FileMenuViewModel<T> : ReactiveObject, IFileMenuViewModel where T : JsonFile<T>
{
    public string Name { get; }

    public FileMenuViewModel(string name, IAppFileHandler<T> fileHandler, DialogueBox dialogue, ILogger logger)
    {
        Name = name;
        _fileHandler = fileHandler;
        _dialogue = dialogue;
        _logger = logger;
        _selected = fileHandler.GetDefault();
        Exportable = _fileHandler is IAppFileHandlerWithImport<T>;

        RefreshList();
        SetupFileWatcher(fileHandler.GetBaseFolder());

        Create = ReactiveCommand.CreateFromTask(OnCreate);
        Import = ReactiveCommand.CreateFromTask(OnImport);

         this.WhenAnyValue(x => x.Selected)
             .InvokeCommand(ReactiveCommand.CreateFromTask<string>(OnSelect));
    }
    private FileSystemWatcher _modWatcher;
    private readonly IAppFileHandler<T> _fileHandler;
    private readonly DialogueBox _dialogue;
    private readonly ILogger _logger;
    private string _selected;
    private bool _isLoading;

    public event AsyncEventHandler<string>? FileSelected; 
    
    public bool Exportable { get; }
    
    public ReactiveCommand<Unit,Unit> Import { get; }
    public ReactiveCommand<Unit,Unit> Create { get; }

    public ObservableCollectionExtended<FileViewModel> List { get; } = [];

    public string Selected
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
                var vm = new FileViewModel(file, file == Selected, Exportable);
                vm.Clicked += OnFileClicked;
                List.Add(vm);
            }
        }
    }

    private async Task OnSelect(string selected)
    {
        foreach (var file in List)
            file.Selected = file.Name == selected;
        
        await OnFileSelected(Selected);
    }

    private async Task OnCreate()
    {
        var name = await GetNewProfileName();
        if (name is null) return;
        _logger.LogInformation(@"Create {name}", name);
        _fileHandler.Create(name);
        Selected = name;
    }

    private async Task OnImport()
    {
        if (_fileHandler is not IAppFileHandlerWithImport<T> importer) return;
        
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

        try
        {
            _logger.LogInformation(@"Importing {file} into {name}", path, name);
            await importer.Import(new FileInfo(path), name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, @"Failed to import");
            await _dialogue.OpenErrorAsync($@"Failed to import the file ({ex.Message})");
        }
    }

    private async Task OnFileClicked(object? sender, FileViewModelEventArgs args)
    {
        switch (args.Action)
        {
            case FileViewAction.Select:
                Selected = args.Name;
                break;
            case FileViewAction.Delete:
                if (await Confirmation(Resources.Deletion, string.Format(Resources.DeletionText, args.Name)))
                {
                    IsLoading = true;
                    _logger.LogInformation(@"Delete {name}", args.Name);
                    await CatchError(@"delete", () => _fileHandler.Delete(args.Name));
                    Selected = _fileHandler.GetDefault();
                    IsLoading = false;
                }
                break;
            case FileViewAction.Duplicate:
                var name = await GetNewProfileName();
                if (name is null) return;
                IsLoading = true;
                _logger.LogInformation(@"Duplicating {name}", name);
                await CatchError(@"duplicate", () => _fileHandler.Duplicate(args.Name, name));
                Selected = name;
                IsLoading = false;
                break;
            case FileViewAction.Export:
                await ExportFile(args.Name);
                break;
            case FileViewAction.Rename:
                var renamed = await GetNewProfileName(args.Name);
                if (renamed is null) return;
                _logger.LogInformation(@"Renaming {old} into {new}", args.Name, renamed);
                IsLoading = true;
                await CatchError(@"rename", () => _fileHandler.Rename(args.Name, renamed));
                Selected = renamed;
                IsLoading = false;
                break;
            case FileViewAction.OpenFolder:
                _logger.LogInformation(@"Opening {name} folder", args.Name);
                string dir = _fileHandler.GetDirectory(args.Name);
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

    private async Task ExportFile(string name)
    {
        if (_fileHandler is not IAppFileHandlerWithImport<T> importer) return;
        
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        if (desktop.MainWindow == null) return;

        var file = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = Resources.SaveFile,
            SuggestedFileName = name,
            FileTypeChoices = [FileType.Json]
        });

        if (file is null) return;
        if (!file.Path.IsFile) return;
        var path = Path.GetFullPath(file.Path.LocalPath);

        try
        {
            _logger.LogInformation(@"Exporting {name} into {file}", name, path);
            await importer.Export(name, new FileInfo(path));
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

    protected virtual async Task OnFileSelected(string args)
    {
        if (FileSelected is not null)
            await FileSelected.Invoke(this, args);
    }
}