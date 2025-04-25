using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData.Binding;
using Humanizer;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using tot_lib;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.ViewModels.InnerContainer;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels;

public class ModListViewModel : ReactiveObject
{
    public ModListViewModel(
        TaskBlocker blocker, 
        ILogger<ModListViewModel> logger, 
        ModFileFactory modFileFactory,
        SteamApi steamApi,
        AppFiles appFiles,
        AppSetup setup,
        IProgressCallback<DepotDownloader.Progress> progress,
        DialogueBox dialogueBox)
    {
        _logger = logger;
        _modFileFactory = modFileFactory;
        _steamApi = steamApi;
        _appFiles = appFiles;
        _setup = setup;
        _dialogueBox = dialogueBox;
        SetupFileWatcher();

        List.CollectionChanged += OnListChanged;
        progress.ProgressChanged += OnProgressChanged;

        blocker.WhenAnyValue(x => x.CanDownloadMods)
            .InvokeCommand(ReactiveCommand.Create<bool>((x) =>
            {
                _modWatcher.EnableRaisingEvents = x;
            }));
        
        _modFileFactory.Removed += RemoveModFile;
        _modFileFactory.Updated += UpdateModFile;
    }
    
    private FileSystemWatcher _modWatcher;
    private readonly ILogger _logger;
    private readonly ModFileFactory _modFileFactory;
    private readonly SteamApi _steamApi;
    private readonly AppFiles _appFiles;
    private readonly AppSetup _setup;
    private readonly DialogueBox _dialogueBox;
    private string _size = string.Empty;
    private bool _isReadOnly = false;

    public event AsyncEventHandler? ModListChanged;
    
    public ObservableCollectionExtended<IModFile> List { get; } = [];

    public string Size
    {
        get => _size;
        set => this.RaiseAndSetIfChanged(ref _size, value);
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => this.RaiseAndSetIfChanged(ref _isReadOnly, value);
    }

    internal async Task SetReadOnly()
    {
        if (_isReadOnly) return;
        IsReadOnly = true;
        await SetList(List.Select(x => x.Export()));
    }

    internal Task UpdateMods()
    {
        return UpdateMods(List.OfType<IPublishedModFile>().Select(x => x.PublishedId).ToList());
    }
    
    internal async Task UpdateMods(List<ulong> mods)
    {
        _logger.LogInformation(@"Updating mods");
        try
        {
            await _steamApi.UpdateMods(mods);
            using(List.SuspendNotifications())
                await _modFileFactory.QueryFromWorkshop(List, IsReadOnly);
        }
        catch (Exception tex)
        {
            _logger.LogError(tex, @"Failed");
            await _dialogueBox.OpenErrorAsync(tex.Message);
        }
    }
    
    internal async Task ForceLoadModList(IEnumerable<string> modList)
    {
        _steamApi.InvalidateCache();
        await SetList(modList);
    }
    
    internal async Task SetList(IEnumerable<string> modList)
    {
        using (List.SuspendNotifications())
        {
            List.Clear();
            List.AddRange(modList.Select(x => _modFileFactory.Create(x, IsReadOnly)));
            await _modFileFactory.QueryFromWorkshop(List, IsReadOnly);
        }
        Size = CalculateModListSize().Bytes().Humanize();
    }
    
    internal async Task AddModFromWorkshop(WorkshopSearchResult mod)
    {
        if (List.Any(x => x is IPublishedModFile pub && pub.PublishedId == mod.PublishedFileId)) return;
        _logger.LogInformation(@"Adding mod {mod} from workshop", mod.PublishedFileId);
        List.Add(await _modFileFactory.Create(mod, IsReadOnly));
        Size = CalculateModListSize().Bytes().Humanize();
    }

    public Task RemoveModFile(IModFile mod)
    {
        _logger.LogInformation(@"Remove mod {mod}", mod.Export());
        List.Remove(mod);
        Size = CalculateModListSize().Bytes().Humanize();
        return Task.CompletedTask;
    }
    
    private Task UpdateModFile(IPublishedModFile mod)
    {
        return UpdateMods([mod.PublishedId]);
    }
    
    private void OnProgressChanged(object? sender, DepotDownloader.Progress e)
    {
        if (!e.IsFile) return;

        foreach (var file in List)
        {
            if(file is IPublishedModFile pub && pub.PublishedId == e.PublishedId)
                file.Progress.Report(e);
        }
    }
    
    private async void OnListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try
        {
            Size = CalculateModListSize().Bytes().Humanize();
            await OnModListChanged();
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, @"Failed");
            await _dialogueBox.OpenErrorAsync(ex.Message);
        }
    }
    
    private long CalculateModListSize()
    {
        return List.Count == 0 ? 0 : List.Select(x => x.FileSize).Aggregate((a, b) => a+b);
    }
    
    private void OnModFileChanged(object sender, FileSystemEventArgs e)
    {
        var fullPath = e.FullPath;
        Dispatcher.UIThread.Invoke(() =>
        {
            var watch = new Stopwatch();
            watch.Start();
            if (!ModListUtil.TryParseDirectory2ModId(fullPath, out var id)) return;
            for (var i = 0; i < List.Count; i++)
            {
                var modFile = List[i];
                if (modFile is not IPublishedModFile published || published.PublishedId != id) continue;
                var path = published.PublishedId.ToString();  
                _appFiles.Mods.ResolveMod(ref path);
                List[i] = _modFileFactory.Create(modFile, path, IsReadOnly);
            }
            watch.Stop();
            using(_logger.BeginScope((@"fullPath", fullPath)))
                _logger.LogDebug(@$"Update time {watch.ElapsedMilliseconds}ms");
        });
    }
    
    [MemberNotNull("_modWatcher")]
    private void SetupFileWatcher()
    {
        if (_modWatcher != null)
            return;

        _logger.LogInformation(@"Starting mod file watcher");
        var path = Path.Combine(_setup.GetWorkshopFolder());
        if (!Directory.Exists(path))
            Tools.CreateDir(path);

        _modWatcher = new FileSystemWatcher(path);
        _modWatcher.NotifyFilter = NotifyFilters.Attributes
                                   | NotifyFilters.CreationTime
                                   | NotifyFilters.DirectoryName
                                   | NotifyFilters.FileName
                                   | NotifyFilters.LastAccess
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.Security
                                   | NotifyFilters.Size;
        _modWatcher.Changed += OnModFileChanged;
        _modWatcher.Created += OnModFileChanged;
        _modWatcher.Deleted += OnModFileChanged;
        _modWatcher.Renamed += OnModFileChanged;
        _modWatcher.IncludeSubdirectories = true;
        _modWatcher.EnableRaisingEvents = true;
    }

    private async Task OnModListChanged()
    {
        if(ModListChanged is not null)
            await ModListChanged.Invoke(this, EventArgs.Empty);
    }
}