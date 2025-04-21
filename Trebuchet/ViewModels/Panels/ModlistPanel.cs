using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
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
using Trebuchet.Utils;
using Trebuchet.ViewModels.InnerContainer;
using Trebuchet.Windows;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetLib.Services.Importer;

namespace Trebuchet.ViewModels.Panels
{
    public class ModlistPanel : ReactiveObject, IRefreshablePanel, IDisplablePanel, IRefreshingPanel
    {
        public ModlistPanel(
            SteamApi steamApi, 
            AppFiles appFiles,
            UIConfig uiConfig, 
            TaskBlocker blocker,
            DialogueBox box,
            ModlistImporter importer,
            WorkshopSearchViewModel workshop,
            ModFileFactory modFileFactory,
            ILogger<ModlistPanel> logger) 
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
            SetupFileWatcher();
            RefreshProfiles();
            
            _selectedModlist = _appFiles.Mods.ResolveProfile(_uiConfig.CurrentModlistProfile);
            _profile = _appFiles.Mods.Get(_selectedModlist);
            _modlistUrl = _profile.SyncURL;
            _serverPassword = _profile.ServerPassword;
            _serverAddress = _profile.ServerAddress;
            _serverPort = _profile.ServerPort <= 0 ? string.Empty : _profile.ServerPort.ToString();

            _modFileFactory.Removed += RemoveModFile;
            _modFileFactory.Updated += UpdateModFile;

            CreateModlistCommand = ReactiveCommand.CreateFromTask(OnModlistCreate);
            DeleteModlistCommand = ReactiveCommand.CreateFromTask(OnModlistDelete);
            DuplicateModlistCommand = ReactiveCommand.CreateFromTask(OnModlistDuplicate);
            ExploreLocalCommand = ReactiveCommand.CreateFromTask(OnExploreLocal);
            ExploreWorkshopCommand = ReactiveCommand.Create(OnExploreWorkshop);
            ExportToJsonCommand = ReactiveCommand.CreateFromTask(OnExportToJson);
            ExportToTxtCommand = ReactiveCommand.CreateFromTask(OnExportToTxt);
            ImportFromFileCommand = ReactiveCommand.CreateFromTask(OnImportFromFile);
            ImportFromTextCommand = ReactiveCommand.CreateFromTask(OnImportFromText);
            FetchCommand = ReactiveCommand.CreateFromTask(OnFetchClicked);
            RefreshModlistCommand = ReactiveCommand.CreateFromTask(ForceLoadModlist);

            var canDownloadMods = blocker.WhenAnyValue(x => x.CanDownloadMods);
            UpdateModsCommand = ReactiveCommand.CreateFromTask(() =>
            {
                return UpdateMods(Modlist.OfType<IPublishedModFile>().Select(x => x.PublishedId).ToList());
            }, canDownloadMods);

            blocker.WhenAnyValue(x => x.CanDownloadMods)
                .InvokeCommand(ReactiveCommand.Create<bool>((x) =>
                {
                    _modWatcher.EnableRaisingEvents = x;
                }));

            this.WhenAnyValue(x => x.ModlistUrl)
                .Subscribe((url) =>
                {
                    if (_swapping) return;
                    _profile.SyncURL = url;
                    _profile.SaveFile();
                });
            this.WhenAnyValue(x => x.ServerAddress, x => x.ServerPort, x => x.ServerPassword)
                .Subscribe((args) =>
                {
                    if (_swapping) return;
                    _profile.ServerAddress = args.Item1;
                    if (int.TryParse(args.Item2, out var port))
                        _profile.ServerPort = port;
                    else
                        _profile.ServerPort = -1;
                    _profile.ServerPassword = args.Item3;
                    _profile.SaveFile();
                });

            _serverDetailsValid = this.WhenAnyValue(x => x.ServerAddress, x => x.ServerPort,
                    (a, p) => 
                        (IPAddress.TryParse(a, out _) || string.IsNullOrEmpty(a)) && 
                        (int.TryParse(p, out var port) && port is >= 0 and <= 65535 || string.IsNullOrEmpty(p))
                    )
                .ToProperty(this, x => x.ServerDetailsValid);
            
            this.WhenAnyValue(x => x.SelectedModlist)
                .InvokeCommand(ReactiveCommand.CreateFromTask<string>(OnModlistChanged));
        }
        
        private readonly SteamApi _steamApi;
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
        private string _modlistUrl;
        private string _selectedModlist;
        private string _modlistSize = string.Empty;
        private bool _canBeOpened = true;
        private string _serverAddress;
        private string _serverPort;
        private string _serverPassword;
        private bool _swapping = false;
        public ObservableAsPropertyHelper<bool> _serverDetailsValid;

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

        public string Icon => @"mdi-toy-brick";
        public string Label => Resources.PanelMods;

        public bool CanBeOpened
        {
            get => _canBeOpened;
            set => this.RaiseAndSetIfChanged(ref _canBeOpened, value);
        }

        public string ModlistUrl
        {
            get => _modlistUrl;
            set => this.RaiseAndSetIfChanged(ref _modlistUrl, value);
        }

        public string ServerAddress
        {
            get => _serverAddress;
            set => this.RaiseAndSetIfChanged(ref _serverAddress, value);
        }

        public string ServerPort
        {
            get => _serverPort;
            set => this.RaiseAndSetIfChanged(ref _serverPort, value);
        }

        public string ServerPassword
        {
            get => _serverPassword;
            set => this.RaiseAndSetIfChanged(ref _serverPassword, value);
        }

        public bool ServerDetailsValid => _serverDetailsValid.Value;

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                var resolved = _appFiles.Mods.ResolveProfile(value);
                this.RaiseAndSetIfChanged(ref _selectedModlist, resolved);
            }
        }

        public string ModlistSize
        {
            get => _modlistSize;
            set => this.RaiseAndSetIfChanged(ref _modlistSize, value);
        }

        public ObservableCollectionExtended<string> Profiles { get; } = [];
        
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
            _logger.LogInformation(@"Remove mod {mod} from list {name}", mod.Export(), SelectedModlist);
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

        private Task OnModlistChanged() => OnModlistChanged(SelectedModlist);
        private async Task OnModlistChanged(string modlist)
        {
            _logger.LogDebug(@"Swap to modlist {modlist}", modlist);
            _uiConfig.CurrentModlistProfile = modlist;
            _uiConfig.SaveFile();
            _profile = _appFiles.Mods.Get(modlist);
            _swapping = true;
            ModlistUrl = _profile.SyncURL;
            ServerAddress = _profile.ServerAddress;
            ServerPort = _profile.ServerPort <= 0 ? string.Empty : _profile.ServerPort.ToString();
            ServerPassword = _profile.ServerPassword;
            _swapping = false;
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

        private async Task FetchJsonList(UriBuilder builder)
        {
            using(_logger.BeginScope((@"url", builder.ToString())))
                _logger.LogInformation(@"Fetching json list");
            
            try
            {
                var result = await Tools.DownloadModList(builder.ToString(), CancellationToken.None);
                var export = _importer.Import(result);
                export.SetValues(_profile);
                _profile.SaveFile();
                await OnModlistChanged();
            }
            catch (Exception tex)
            {
                _logger.LogError(tex, @"Failed");
                await _box.OpenErrorAsync(tex.Message);
            }

        }

        private async Task FetchSteamCollection(UriBuilder builder)
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

        private async Task OnExploreLocal()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (desktop.MainWindow == null) return;

            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Resources.AddLocalMod,
                FileTypeFilter = [FileType.Pak],
                AllowMultiple = true
            });

            _logger.BeginScope(@"Adding local mods");
            Modlist.AddRange(
                files.Where(f => f.Path.IsFile)
                    .Select(f => Path.GetFullPath(f.Path.LocalPath))
                    .Select(f => _modFileFactory.Create(f))
                );
            _profile.Modlist = Modlist.Select(x => x.Export()).ToList();
            _profile.SaveFile();
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

        private async Task OnExportToJson()
        {
            var json = _importer.Export(_profile, ImportFormats.Json);
            var editor = new OnBoardingModlistImport(json, true, FileType.Json);
            await _box.OpenAsync(editor);
        }

        private async Task OnExportToTxt()
        {
            try
            {
                var content = _importer.Export(_profile, ImportFormats.Txt);
                var editor = new OnBoardingModlistImport(content, true, FileType.Txt);
                await _box.OpenAsync(editor);
            }
            catch
            {
                await _box.OpenErrorAsync(Resources.ExportErrorModNotFound);
            }
        }

        private async Task OnFetchClicked()
        {
            if (string.IsNullOrEmpty(ModlistUrl)) return;

            _logger.LogInformation(@"Sync modlist");
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
            catch(Exception ex)
            {
                _logger.LogWarning(ex, @"Failed");
                await _box.OpenErrorAsync(Resources.InvalidURL);
                return;
            }

            if (SteamWorks.SteamCommunityHost == builder.Host)
                await FetchSteamCollection(builder);
            else
                await FetchJsonList(builder);
        }

        private async Task OnImportFromFile()
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
                await OnImportFromText(content);
            
        }

        private async Task OnImportFromText()
        {
            var clipboard = await Utils.Utils.GetClipBoard();
            if (_importer.GetFormat(clipboard) == ImportFormats.Invalid)
                clipboard = string.Empty;
            
            await OnImportFromText(clipboard);
        }

        private async Task OnImportFromText(string import)
        {
            var editor = new OnBoardingModlistImport(import, false, FileType.Json);
            await _box.OpenAsync(editor);
            if (editor.Canceled || editor.Value == null) return;

            _logger.LogInformation(@"Importing modlist");
            var format = _importer.GetFormat(import);
            var text = editor.Value;
            try
            {
                var export = _importer.Import(text);
                if (format == ImportFormats.Txt)
                {
                    if (editor.Append)
                        _profile.Modlist.AddRange(export.Modlist);
                    else
                        _profile.Modlist = export.Modlist;
                }
                else
                {
                    if(editor.Append)
                        _profile.Modlist.AddRange(export.Modlist);
                    else
                    {
                        export.SetValues(_profile);
                    }
                }
                    
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, @"Failed");
                await _box.OpenErrorAsync(Resources.WrongType, Resources.WrongTypeText);
                return;
            }

            _profile.SaveFile();
            await OnModlistChanged();
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

        private async Task OnModlistCreate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
            _logger.LogInformation(@"Create modlist {name}", name);
            _appFiles.Mods.Create(name);
            RefreshProfiles();
            SelectedModlist = name;
        }

        private async Task OnModlistDelete()
        {
            if (string.IsNullOrEmpty(SelectedModlist)) return;

            OnBoardingConfirmation confirm = new OnBoardingConfirmation(
                Resources.Deletion,
                string.Format(Resources.DeletionText, SelectedModlist));
            await _box.OpenAsync(confirm);
            if (!confirm.Result) return;
            
            _logger.LogInformation(@"Modlist delete {name}", SelectedModlist);
            _appFiles.Mods.Delete(SelectedModlist);

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

        private async Task OnModlistDuplicate()
        {
            var name = await GetNewProfileName();
            if (name is null) return;
            _logger.LogInformation(@"Modlist duplicate {from} to {to}", _profile.ProfileName, name);
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

        [MemberNotNull("_modWatcher")]
        private void SetupFileWatcher()
        {
            if (_modWatcher != null)
                return;

            _logger.LogInformation(@"Starting mod file watcher");
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


        private async Task OnRequestRefresh()
        {
            if(RequestRefresh is not null)
                await RequestRefresh.Invoke(this, EventArgs.Empty);
        }
    }
}