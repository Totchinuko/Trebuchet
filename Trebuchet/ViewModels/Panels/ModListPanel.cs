using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using tot_lib;
using Trebuchet.Assets;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.Windows;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetLib.Services.Importer;

namespace Trebuchet.ViewModels.Panels
{
    public class ModListPanel : ReactiveObject, IRefreshablePanel, IDisplablePanel, IRefreshingPanel
    {
        public ModListPanel(
            ModListViewModel modList,
            AppFiles appFiles,
            UIConfig uiConfig, 
            TaskBlocker blocker,
            DialogueBox box,
            ModlistImporter importer,
            WorkshopSearchViewModel workshop,
            ILogger<ModListPanel> logger)
        {
            ModList = modList;
            ModList.ModListChanged += OnModListChanged;
            
            _appFiles = appFiles;
            _uiConfig = uiConfig;
            _box = box;
            _importer = importer;
            _workshop = workshop;
            _logger = logger;
            _workshop.ModAdded += (_,mod) => ModList.AddModFromWorkshop(mod);
            
            var startingFile = _appFiles.Mods.Resolve(_uiConfig.CurrentModlistProfile);
            FileMenu = new FileMenuViewModel<ModListProfile>(Resources.PanelMods, appFiles.Mods, box, logger);
            FileMenu.FileSelected += OnFileSelected;
            FileMenu.Selected = startingFile;
            
            _profile = _appFiles.Mods.Get(startingFile);

            Workshop = ReactiveCommand.Create(OnExploreWorkshop);
            EditAsText = ReactiveCommand.CreateFromTask(OnEditModListAsText);
            Sync = ReactiveCommand.CreateFromTask(OnSync);
            SyncEdit = ReactiveCommand.CreateFromTask(OnSyncEdit);
            RefreshList = ReactiveCommand.CreateFromTask(() => ModList.ForceLoadModList(_profile.Modlist));

            var canDownloadMods = blocker.WhenAnyValue(x => x.CanDownloadMods);
            Update = ReactiveCommand.CreateFromTask(async () =>
            {
                await ModList.UpdateMods();
                await OnRequestRefresh();
            }, canDownloadMods);
  
        }
        private readonly AppFiles _appFiles;
        private readonly UIConfig _uiConfig;
        private readonly DialogueBox _box;
        private readonly ModlistImporter _importer;
        private readonly WorkshopSearchViewModel _workshop;
        private readonly ILogger<ModListPanel> _logger;
        private bool _needRefresh;
        private ModListProfile _profile;
        private WorkshopSearch? _searchWindow;
        private bool _canBeOpened = true;

        public ReactiveCommand<Unit, Unit> Workshop { get; }
        public ReactiveCommand<Unit, Unit> EditAsText { get; }
        public ReactiveCommand<Unit, Unit> Sync { get; }
        public ReactiveCommand<Unit, Unit> SyncEdit { get; }
        public ReactiveCommand<Unit, Unit> Update { get; }
        public ReactiveCommand<Unit, Unit> RefreshList { get; }
        
        public IFileMenuViewModel FileMenu { get; }
        
        public ModListViewModel ModList { get; }

        public string Icon => @"mdi-toy-brick";
        public string Label => Resources.PanelMods;

        public bool CanBeOpened
        {
            get => _canBeOpened;
            set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
        }

        public event AsyncEventHandler? RequestRefresh;
        
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
            await ModList.SetList(_profile.Modlist);
        }
        
        private Task OnModListChanged(object? sender, EventArgs args)
        {
            _profile.Modlist = ModList.List.Select(x => x.Export()).ToList();
            _profile.SaveFile();
            return Task.CompletedTask;
        }
        
        
        private Task OnFileChanged() => OnFileSelected(this, FileMenu.Selected);
        private async Task OnFileSelected(object? sender, string profile)
        {
            _logger.LogDebug(@"Swap to mod list {modList}", profile);
            _uiConfig.CurrentModlistProfile = profile;
            _uiConfig.SaveFile();
            _profile = _appFiles.Mods.Get(profile);
            await ModList.SetList(_profile.Modlist);
        }

        private async Task SyncJson(UriBuilder builder)
        {
            using(_logger.BeginScope((@"url", builder.ToString())))
                _logger.LogInformation(@"Fetching json list");
            
            try
            {
                var result = await Tools.DownloadModList(builder.ToString(), CancellationToken.None);
                await _appFiles.Mods.Import(result, FileMenu.Selected);
                await OnFileChanged();
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

                await ModList.SetList(result.CollectionDetails
                    .First()
                    .Children.Select(x => x.PublishedFileId));
            }
            catch (Exception tex)
            {
                _logger.LogError(tex, @"Failed");
                await _box.OpenErrorAsync(tex.Message);
            }
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
     
        private async Task OnEditModListAsText()
        {
            var modList = _appFiles.Mods.GetResolvedModlist(_profile.Modlist, false);
            var editor = new OnBoardingModlistImport(string.Join(Environment.NewLine, modList));
            
            while (true)
            {
                await _box.OpenAsync(editor);
                if (editor.Value is null) return;
                try
                {
                    var parsed = _importer.Import(editor.Value, ImportFormats.Txt);
                    await ModList.SetList(parsed.Modlist);
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
            _logger.LogInformation(@"Sync modList");
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

        private void OnSearchClosing(object? sender, CancelEventArgs e)
        {
            if (_searchWindow == null) return;
            _searchWindow.Closing -= OnSearchClosing;
            _searchWindow = null;
        }

        private async Task OnRequestRefresh()
        {
            if(RequestRefresh is not null)
                await RequestRefresh.Invoke(this, EventArgs.Empty);
        }
    }
}