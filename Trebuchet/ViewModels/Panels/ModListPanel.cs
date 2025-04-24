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
        
        private async Task OnFileSelected(object? sender, string profile)
        {
            _logger.LogDebug(@"Swap to mod list {modList}", profile);
            _uiConfig.CurrentModlistProfile = profile;
            _uiConfig.SaveFile();
            _profile = _appFiles.Mods.Get(profile);
            await ModList.SetList(_profile.Modlist);
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