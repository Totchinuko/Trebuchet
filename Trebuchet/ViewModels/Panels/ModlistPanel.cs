using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using Trebuchet.Assets;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.Utils;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.Windows;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetLib.Services.Importer;

namespace Trebuchet.ViewModels.Panels
{
    public class ModlistPanel : Panel
    {
        private readonly SteamAPI _steamApi;
        private readonly AppFiles _appFiles;
        private readonly UIConfig _uiConfig;
        private readonly DialogueBox _box;
        private readonly ModlistImporter _importer;
        private readonly WorkshopSearchViewModel _workshop;
        private readonly ModFileFactory _modFileFactory;
        private readonly ILogger<ModlistPanel> _logger;
        private FileSystemWatcher? _modWatcher;
        private bool _needRefresh;
        private ModListProfile _profile;
        private WorkshopSearch? _searchWindow;
        private string _modlistUrl = string.Empty;
        private string _selectedModlist = string.Empty;

        public ModlistPanel(
            SteamAPI steamApi, 
            AppFiles appFiles,
            UIConfig uiConfig, 
            TaskBlocker blocker,
            DialogueBox box,
            ModlistImporter importer,
            WorkshopSearchViewModel workshop,
            ModFileFactory modFileFactory,
            ILogger<ModlistPanel> logger) : 
            base(Resources.PanelMods, "mdi-toy-brick", false)
        {
            _steamApi = steamApi;
            _appFiles = appFiles;
            _uiConfig = uiConfig;
            _box = box;
            _importer = importer;
            _workshop = workshop;
            _modFileFactory = modFileFactory;
            _logger = logger;
            _workshop.ModAdded += (_,mod) => AddModFromWorkshop(mod);
            LoadPanel();

            _modFileFactory.Removed += RemoveModFile;
            _modFileFactory.Updated += UpdateModFile;

            CreateModlistCommand = ReactiveCommand.Create(OnModlistCreate);
            DeleteModlistCommand = ReactiveCommand.Create(OnModlistDelete);
            DuplicateModlistCommand = ReactiveCommand.Create(OnModlistDuplicate);
            ExploreLocalCommand = ReactiveCommand.Create(OnExploreLocal);
            ExploreWorkshopCommand = ReactiveCommand.Create(OnExploreWorkshop);
            ExportToJsonCommand = ReactiveCommand.Create(OnExportToJson);
            ExportToTxtCommand = ReactiveCommand.Create(OnExportToTxt);
            ImportFromFileCommand = ReactiveCommand.Create(OnImportFromFile);
            ImportFromTextCommand = ReactiveCommand.Create(OnImportFromText);
            FetchCommand = ReactiveCommand.Create(OnFetchClicked);
            RefreshModlistCommand = ReactiveCommand.Create(ForceLoadModlist);

            var canDownloadMods = blocker.WhenAnyValue(x => x.CanDownloadMods);
            UpdateModsCommand = ReactiveCommand.Create(() =>
            {
                UpdateMods(Modlist.OfType<IPublishedModFile>().Select(x => x.PublishedId).ToList());
            }, canDownloadMods);

            TabClick.Subscribe((_) =>
            {
                if (!_needRefresh) return;
                _needRefresh = false;
                LoadPanel();
            });
            RefreshPanel.Subscribe((_) => _needRefresh = true);

            this.WhenAnyValue(x => x.ModlistUrl)
                .Subscribe((url) =>
                {
                    _profile.SyncURL = url;
                    _profile.SaveFile();
                });
            
            this.WhenAnyValue(x => x.SelectedModlist)
                .Subscribe((list) =>
                {
                    SelectedModlist = _appFiles.Mods.ResolveProfile(list);
                    _uiConfig.CurrentModlistProfile = SelectedModlist;
                    _uiConfig.SaveFile();
                    LoadProfile();
                });
        }

        public ReactiveCommand<Unit, Unit> CreateModlistCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteModlistCommand { get; }
        public ReactiveCommand<Unit, Unit> DuplicateModlistCommand { get; }
        public ReactiveCommand<Unit, Unit> ExploreLocalCommand { get; }
        public ReactiveCommand<Unit, Unit> ExploreWorkshopCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportToJsonCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportToTxtCommand { get; }
        public ReactiveCommand<Unit, Unit> FetchCommand { get; }
        public ReactiveCommand<Unit, Unit> ImportFromFileCommand { get; }
        public ReactiveCommand<Unit, Unit> ImportFromTextCommand { get; }
        public ReactiveCommand<Unit, Unit> UpdateModsCommand { get; }
        public ReactiveCommand<Unit, Unit> RefreshModlistCommand { get; }

        public ObservableCollectionExtended<IModFile> Modlist { get; } = [];

        public string ModlistUrl
        {
            get => _modlistUrl;
            set => this.RaiseAndSetIfChanged(ref _modlistUrl, value);
        }

        public string SelectedModlist
        {
            get => _selectedModlist;
            set => this.RaiseAndSetIfChanged(ref _selectedModlist, value);
        }

        public ObservableCollectionExtended<string> Profiles { get; } = [];

       
        public async void UpdateMods(List<ulong> mods)
        {
            try
            {
                await _steamApi.UpdateMods(mods);
                await _modFileFactory.QueryFromWorkshop(Modlist);
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await _box.OpenErrorAsync(tex.Message);
            }
        }
        
        public async void AddModFromWorkshop(WorkshopSearchResult mod)
        {
            if (Modlist.Any(x => x is IPublishedModFile pub && pub.PublishedId == mod.PublishedFileId)) return;
            var file = await _modFileFactory.Create(mod);
            Modlist.Add(file);
            _profile.Modlist = Modlist.Select(x => x.Export()).ToList();
            _profile.SaveFile();
        }

        public void RemoveModFile(IModFile mod)
        {
            Modlist.Remove(mod);
            _profile.Modlist = Modlist.Select(x => x.Export()).ToList();
            _profile.SaveFile();
        }

        public void UpdateModFile(IPublishedModFile mod)
        {
            UpdateMods([mod.PublishedId]);
        }

        private async void FetchJsonList(UriBuilder builder)
        {
            try
            {
                var result = await Tools.DownloadModList(builder.ToString(), CancellationToken.None);
                _profile.Modlist = _appFiles.Mods.ParseModList(result.Modlist).ToList();
                _profile.SaveFile();
                LoadModlist();
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await _box.OpenErrorAsync(tex.Message);
            }

        }

        private async void FetchSteamCollection(UriBuilder builder)
        {
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
                LoadModlist();
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await _box.OpenErrorAsync(tex.Message);
            }
        }

        private void ForceLoadModlist()
        {
            _steamApi.InvalidateCache();
            LoadModlist();
        }

        private async void LoadModlist()
        {
            Modlist.Clear();
            foreach (var mod in _profile.Modlist)
                Modlist.Add(_modFileFactory.Create(mod));
            await _modFileFactory.QueryFromWorkshop(Modlist);
        }

        [MemberNotNull("_profile")]
        private void LoadPanel()
        {
            SetupFileWatcher();
            SelectedModlist = _appFiles.Mods.ResolveProfile(_uiConfig.CurrentModlistProfile);
            LoadProfile();
            RefreshProfiles();
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = _appFiles.Mods.Get(SelectedModlist);
            ModlistUrl = _profile.SyncURL;
            LoadModlist();
        }

        private async void OnExploreLocal()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (desktop.MainWindow == null) return;

            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Resources.AddLocalMod,
                FileTypeFilter = [FileType.Pak],
                AllowMultiple = true
            });

            Modlist.AddRange(
                files.Where(f => f.Path.IsFile)
                    .Select(f => Path.GetFullPath(f.Path.LocalPath))
                    .Select(f => _modFileFactory.Create(f))
                );
            _profile.Modlist = Modlist.Select(x => x.Export()).ToList();
            _profile.SaveFile();
        }

        private void OnExploreWorkshop()
        {
            if (_searchWindow != null) return;
            _searchWindow = new WorkshopSearch();
            _searchWindow.DataContext = _workshop;
            _searchWindow.Closing += OnSearchClosing;
            _searchWindow.Show();
        }

        private async void OnExportToJson()
        {
            var json = _importer.Export(_profile.Modlist, ImportFormats.Json);
            var editor = new OnBoardingModlistImport(json, true, FileType.Json);
            await _box.OpenAsync(editor);
            
        }

        private async void OnExportToTxt()
        {
            try
            {
                var content = _importer.Export(_profile.Modlist, ImportFormats.Txt);
                var editor = new OnBoardingModlistImport(content, true, FileType.Txt);
                await _box.OpenAsync(editor);
            }
            catch
            {
                await _box.OpenErrorAsync(Resources.ExportErrorModNotFound);
            }
        }

        private async void OnFetchClicked()
        {
            if (string.IsNullOrEmpty(ModlistUrl)) return;

            OnBoardingConfirmation confirm = new OnBoardingConfirmation(
                Resources.ModlistReplace,
                string.Format(Resources.ModlistReplaceText, SelectedModlist));
            await _box.OpenAsync(confirm);
            if (!confirm.Result) return;

            UriBuilder builder;
            try
            {
                builder = new UriBuilder(ModlistUrl);
            }
            catch
            {
                await _box.OpenErrorAsync(Resources.InvalidURL);
                return;
            }

            if (SteamWorks.SteamCommunityHost == builder.Host)
                FetchSteamCollection(builder);
            else
                FetchJsonList(builder);
        }

        private async void OnImportFromFile()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (desktop.MainWindow == null) return;

            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = Resources.ImportModList,
                FileTypeFilter = [FileType.Json, FileType.Txt],
                SuggestedFileName = @"modlist.txt"
            });

            if (files.Count <= 0) return;
            if (!files[0].Path.IsFile) return;
            var file = files[0].Path.LocalPath;
            var path = Path.GetFullPath(file);

            var content = await File.ReadAllTextAsync(path);
            if (_importer.GetFormat(content) == ImportFormats.Invalid)
                await _box.OpenErrorAsync(Resources.WrongType, Resources.WrongTypeText);
            else
                OnImportFromText(content);
            
        }

        private async void OnImportFromText()
        {
            var clipboard = await Utils.Utils.GetClipBoard();
            if (_importer.GetFormat(clipboard) == ImportFormats.Invalid)
                clipboard = string.Empty;
            
            OnImportFromText(clipboard);
        }

        private async void OnImportFromText(string import)
        {
            var editor = new OnBoardingModlistImport(import, false, FileType.Json);
            await _box.OpenAsync(editor);
            if (editor.Canceled || editor.Value == null) return;

            var text = editor.Value;
            List<string> modlist = [];
            try
            {
                modlist.AddRange(_importer.Import(text));
            }
            catch
            {
                await _box.OpenErrorAsync(Resources.WrongType, Resources.WrongTypeText);
                return;
            }

            if (editor.Append)
                _profile.Modlist.AddRange(modlist);
            else
                _profile.Modlist = modlist;
            _profile.SaveFile();
            LoadModlist();
        }

        private void OnModFileChanged(object sender, FileSystemEventArgs e)
        {
            var fullPath = e.FullPath;
            Debug.WriteLine(@$"OnModFileChanged.fullPath={fullPath}");
            Dispatcher.UIThread.Invoke(() =>
            {
                var watch = new Stopwatch();
                watch.Start();
                if (!_appFiles.Mods.TryParseDirectory2ModID(fullPath, out var id)) return;
                for (var i = 0; i < Modlist.Count; i++)
                {
                    var modFile = Modlist[i];
                    if (modFile is not IPublishedModFile published || published.PublishedId != id) continue;
                    var path = published.PublishedId.ToString();
                    _appFiles.Mods.ResolveMod(ref path);
                    Modlist[i] = _modFileFactory.Create(modFile, path);
                }
                watch.Stop();
                Debug.WriteLine(@$"OnModFileChanged={watch.ElapsedMilliseconds}ms");
            });
        }

        private async void OnModlistCreate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
            _appFiles.Mods.Create(name);
            RefreshProfiles();
            SelectedModlist = name;
        }

        private async void OnModlistDelete()
        {
            if (string.IsNullOrEmpty(SelectedModlist)) return;

            OnBoardingConfirmation confirm = new OnBoardingConfirmation(
                Resources.Deletion,
                string.Format(Resources.DeletionText, SelectedModlist));
            await _box.OpenAsync(confirm);
            if (!confirm.Result) return;
            
            _appFiles.Mods.Delete(_profile.ProfileName);

            RefreshProfiles();
            SelectedModlist = string.Empty;
        }
        
        private Validation ValidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Validation.Invalid(Resources.ErrorNameEmpty);
            if (Profiles.Contains(name))
                return Validation.Invalid(Resources.ErrorNameAlreadyTaken);
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return Validation.Invalid(Resources.ErrorNameInvalidCharacters);
            return Validation.Valid;
        }
        
        private async Task<string?> GetNewProfileName()
        {
            var modal = new OnBoardingNameSelection(Resources.Create, string.Empty)
                .SetValidation(ValidateName);
            await _box.OpenAsync(modal);
            return modal.Value;
        }

        private async void OnModlistDuplicate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
            _profile = _appFiles.Mods.Duplicate(_profile.ProfileName, name);
            RefreshProfiles();
            SelectedModlist = name;
        }

        private void OnSearchClosing(object? sender, CancelEventArgs e)
        {
            if (_searchWindow == null) return;
            _searchWindow.Closing -= OnSearchClosing;
            _searchWindow = null;
        }

        private void RefreshProfiles()
        {
            Profiles.Clear();
            Profiles.AddRange(_appFiles.Mods.ListProfiles());
        }

        private void SetupFileWatcher()
        {
            if (_modWatcher != null)
            {
                _modWatcher.Dispose();
                _modWatcher = null;
            }

            var path = Path.Combine(_appFiles.Mods.GetWorkshopFolder());
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


    }
}