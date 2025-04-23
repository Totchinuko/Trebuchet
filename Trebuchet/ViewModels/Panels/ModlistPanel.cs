using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Threading;
using DynamicData.Binding;
using Humanizer;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using tot_lib;
using Trebuchet.Assets;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.Windows;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetLib.Services.Importer;
using Progress = DepotDownloader.Progress;

namespace Trebuchet.ViewModels.Panels
{
    public class ModlistPanel : ReactiveObject, IRefreshablePanel, IDisplablePanel, IRefreshingPanel
    {
        public ModlistPanel(
            SteamApi steamApi,
            AppSetup setup,
            AppFiles appFiles,
            UIConfig uiConfig, 
            TaskBlocker blocker,
            DialogueBox box,
            ModlistImporter importer,
            WorkshopSearchViewModel workshop,
            ModFileFactory modFileFactory,
            IProgressCallback<DepotDownloader.Progress> progress,
            ILogger<ModlistPanel> logger) 
        {
            _steamApi = steamApi;
            _setup = setup;
            _appFiles = appFiles;
            _uiConfig = uiConfig;
            _box = box;
            _importer = importer;
            _workshop = workshop;
            _modFileFactory = modFileFactory;
            _logger = logger;
            _workshop.ModAdded += (_,mod) => AddModFromWorkshop(mod);
            SetupFileWatcher();

            progress.ProgressChanged += OnProgressChanged;
            
            var startingFile = _appFiles.Mods.Resolve(_uiConfig.CurrentModlistProfile);
            FileMenu = new FileMenuViewModel<ModListProfile>(Resources.PanelMods, appFiles.Mods, box, logger);
            FileMenu.FileSelected += OnFileSelected;
            FileMenu.Selected = startingFile;
            
            _profile = _appFiles.Mods.Get(startingFile);

            _modFileFactory.Removed += RemoveModFile;
            _modFileFactory.Updated += UpdateModFile;

            Workshop = ReactiveCommand.Create(OnExploreWorkshop);
            EditAsText = ReactiveCommand.CreateFromTask(OnEditModlistAsText);
            Sync = ReactiveCommand.CreateFromTask(OnSync);
            SyncEdit = ReactiveCommand.CreateFromTask(OnSyncEdit);
            RefreshList = ReactiveCommand.CreateFromTask(ForceLoadModlist);

            var canDownloadMods = blocker.WhenAnyValue(x => x.CanDownloadMods);
            Update = ReactiveCommand.CreateFromTask(() =>
            {
                return UpdateMods(Modlist.OfType<IPublishedModFile>().Select(x => x.PublishedId).ToList());
            }, canDownloadMods);

            blocker.WhenAnyValue(x => x.CanDownloadMods)
                .InvokeCommand(ReactiveCommand.Create<bool>((x) =>
                {
                    _modWatcher.EnableRaisingEvents = x;
                }));
        }

        private readonly SteamApi _steamApi;
        private readonly AppSetup _setup;
        private readonly AppFiles _appFiles;
        private readonly UIConfig _uiConfig;
        private readonly DialogueBox _box;
        private readonly ModlistImporter _importer;
        private readonly WorkshopSearchViewModel _workshop;
        private readonly ModFileFactory _modFileFactory;
        private readonly ILogger<ModlistPanel> _logger;
        private FileSystemWatcher _modWatcher;
        private bool _needRefresh;
        private ModListProfile _profile;
        private WorkshopSearch? _searchWindow;
        private string _modlistSize = string.Empty;
        private bool _canBeOpened = true;

        public ReactiveCommand<Unit, Unit> Workshop { get; }
        public ReactiveCommand<Unit, Unit> EditAsText { get; }
        public ReactiveCommand<Unit, Unit> Sync { get; }
        public ReactiveCommand<Unit, Unit> SyncEdit { get; }
        public ReactiveCommand<Unit, Unit> Update { get; }
        public ReactiveCommand<Unit, Unit> RefreshList { get; }

        public ObservableCollectionExtended<IModFile> Modlist { get; } = [];
        
        public IFileMenuViewModel FileMenu { get; }

        public string Icon => @"mdi-toy-brick";
        public string Label => Resources.PanelMods;

        public bool CanBeOpened
        {
            get => _canBeOpened;
            set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
        }

        public string ModlistSize
        {
            get => _modlistSize;
            set => this.RaiseAndSetIfChanged(ref _modlistSize, value);
        }

        public event AsyncEventHandler? RequestRefresh;
        
        public async Task AddModFromWorkshop(WorkshopSearchResult mod)
        {
            if (Modlist.Any(x => x is IPublishedModFile pub && pub.PublishedId == mod.PublishedFileId)) return;
            _logger.LogInformation(@"Adding mod {mod} from workshop", mod.PublishedFileId);
            var file = await _modFileFactory.Create(mod);
            Modlist.Add(file);
            _profile.Modlist = Modlist.Select(x => x.Export()).ToList();
            _profile.SaveFile();
            ModlistSize = CalculateModlistSize().Bytes().Humanize();
        }

        public Task RemoveModFile(IModFile mod)
        {
            _logger.LogInformation(@"Remove mod {mod} from list {name}", mod.Export(), FileMenu.Selected);
            Modlist.Remove(mod);
            _profile.Modlist = Modlist.Select(x => x.Export()).ToList();
            _profile.SaveFile();
            ModlistSize = CalculateModlistSize().Bytes().Humanize();
            return Task.CompletedTask;
        }

        public Task UpdateModFile(IPublishedModFile mod)
        {
            return UpdateMods([mod.PublishedId]);
        }

        public Task RefreshPanel()
        {
            _logger.LogDebug(@"Refresh panel");
            _needRefresh = true;
            return Task.CompletedTask;
        }

        public async Task DisplayPanel()
        {
            _logger.LogDebug(@"Display panel");
            if (!_needRefresh) return;
            _needRefresh = false;
            await LoadModlist();
        }
        
        private void OnProgressChanged(object? sender, Progress e)
        {
            if (!e.IsFile) return;

            foreach (var file in Modlist)
            {
                if(file is IPublishedModFile pub && pub.PublishedId == e.PublishedId)
                    file.Progress.Report(e);
            }
        }
        
        private Task OnModlistChanged() => OnFileSelected(this, FileMenu.Selected);
        private async Task OnFileSelected(object? sender, string profile)
        {
            _logger.LogDebug(@"Swap to modlist {modlist}", profile);
            _uiConfig.CurrentModlistProfile = profile;
            _uiConfig.SaveFile();
            _profile = _appFiles.Mods.Get(profile);
            await LoadModlist();
        }

        private long CalculateModlistSize()
        {
            return Modlist.Count == 0 ? 0 : Modlist.Select(x => x.FileSize).Aggregate((a, b) => a+b);
        }

        private async Task UpdateMods(List<ulong> mods)
        {
            _logger.LogInformation(@"Updating mods");
            try
            {
                await _steamApi.UpdateMods(mods);
                await _modFileFactory.QueryFromWorkshop(Modlist);
                await OnRequestRefresh();
            }
            catch (Exception tex)
            {
                _logger.LogError(tex, @"Failed");
                await _box.OpenErrorAsync(tex.Message);
            }
        }

        private async Task SyncJson(UriBuilder builder)
        {
            using(_logger.BeginScope((@"url", builder.ToString())))
                _logger.LogInformation(@"Fetching json list");
            
            try
            {
                var result = await Tools.DownloadModList(builder.ToString(), CancellationToken.None);
                await _appFiles.Mods.Import(result, FileMenu.Selected);
                await OnModlistChanged();
            }
            catch (Exception tex)
            {
                _logger.LogError(tex, @"Failed");
                await _box.OpenErrorAsync(tex.Message);
            }

        }

        private async Task SyncSteamCollection(UriBuilder builder)
        {
            using(_logger.BeginScope((@"url", builder.ToString())))
                _logger.LogInformation(@"Fetching steam collection");
            
            var query = HttpUtility.ParseQueryString(builder.Query);
            var id = query.Get(@"id");
            if (id == null || !ulong.TryParse(id, out var collectionId))
            {
                await _box.OpenErrorAsync(Resources.InvalidURL, Resources.InvalidURLText);
                return;
            }

            try
            {
                var result = await SteamRemoteStorage.GetCollectionDetails(
                    new GetCollectionDetailsQuery(collectionId), CancellationToken.None);
                var modlist = new List<string>();
                foreach (var child in result.CollectionDetails.First().Children)
                    modlist.Add(child.PublishedFileId);
                _profile.Modlist = modlist;
                _profile.SaveFile();
                await OnModlistChanged();
            }
            catch (Exception tex)
            {
                _logger.LogError(tex, @"Failed");
                await _box.OpenErrorAsync(tex.Message);
            }
        }

        private async Task ForceLoadModlist()
        {
            _steamApi.InvalidateCache();
            await LoadModlist();
        }

        private async Task LoadModlist()
        {
            Modlist.Clear();
            foreach (var mod in _profile.Modlist)
                Modlist.Add(_modFileFactory.Create(mod));
            await _modFileFactory.QueryFromWorkshop(Modlist);
            ModlistSize = CalculateModlistSize().Bytes().Humanize();
        }

        private void OnExploreWorkshop()
        {
            if (_searchWindow != null) return;
            _searchWindow = new ()
            {
                DataContext = _workshop
            };
            _searchWindow.Closing += OnSearchClosing;
            _searchWindow.Show();
        }
     
        private async Task OnEditModlistAsText()
        {
            var modlist = _appFiles.Mods.GetResolvedModlist(_profile.Modlist, false);
            var editor = new OnBoardingModlistImport(string.Join(Environment.NewLine, modlist));
            
            while (true)
            {
                await _box.OpenAsync(editor);
                if (editor.Value is null) return;
                try
                {
                    var parsed = _importer.Import(editor.Value, ImportFormats.Txt);
                    _profile.Modlist = parsed.Modlist;
                    _profile.SaveFile();
                    await OnModlistChanged();
                    return;
                }
                catch(Exception ex)
                {
                    await _box.OpenErrorAsync(ex.Message);
                }
            }
        }

        private async Task OnSync()
        {
            _logger.LogInformation(@"Sync modlist");
            OnBoardingConfirmation confirm = new OnBoardingConfirmation(
                Resources.ModlistReplace,
                string.Format(Resources.ModlistReplaceText, FileMenu.Selected));
            await _box.OpenAsync(confirm);
            if (!confirm.Result) return;

            if (string.IsNullOrEmpty(_profile.SyncURL))
                await OnSyncEdit();

            UriBuilder builder;
            try
            {
                builder = new UriBuilder(_profile.SyncURL);
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, @"Failed");
                await _box.OpenErrorAsync(Resources.InvalidURL);
                return;
            }

            if (SteamWorks.SteamCommunityHost == builder.Host)
                await SyncSteamCollection(builder);
            else
                await SyncJson(builder);
        }

        private async Task OnSyncEdit()
        {
            var editor = new OnBoardingNameSelection(Resources.Sync, Resources.SyncText);
            editor.Value = _profile.SyncURL;
            editor.Watermark = @"https://";
            await _box.OpenAsync(editor);
            if (editor.Value is null) return;
            _profile.SyncURL = editor.Value;
            _profile.SaveFile();
        }

        private void OnModFileChanged(object sender, FileSystemEventArgs e)
        {
            var fullPath = e.FullPath;
            Dispatcher.UIThread.Invoke(() =>
            {
                var watch = new Stopwatch();
                watch.Start();
                if (!_appFiles.Mods.TryParseDirectory2ModId(fullPath, out var id)) return;
                for (var i = 0; i < Modlist.Count; i++)
                {
                    var modFile = Modlist[i];
                    if (modFile is not IPublishedModFile published || published.PublishedId != id) continue;
                    var path = published.PublishedId.ToString();
                    _appFiles.Mods.ResolveMod(ref path);
                    Modlist[i] = _modFileFactory.Create(modFile, path);
                }
                watch.Stop();
                using(_logger.BeginScope((@"fullPath", fullPath)))
                    _logger.LogDebug(@$"Update time {watch.ElapsedMilliseconds}ms");
            });
        }

        private void OnSearchClosing(object? sender, CancelEventArgs e)
        {
            if (_searchWindow == null) return;
            _searchWindow.Closing -= OnSearchClosing;
            _searchWindow = null;
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


        private async Task OnRequestRefresh()
        {
            if(RequestRefresh is not null)
                await RequestRefresh.Invoke(this, EventArgs.Empty);
        }
    }
}