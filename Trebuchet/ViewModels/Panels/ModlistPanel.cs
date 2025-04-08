using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading;
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
using Trebuchet.Modals;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.Utils;
using Trebuchet.Windows;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils.Modals;

namespace Trebuchet.ViewModels.Panels
{
    public class ModlistPanel : Panel
    {
        private readonly SteamAPI _steamApi;
        private readonly AppFiles _appFiles;
        private readonly UIConfig _uiConfig;
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
            WorkshopSearchViewModel workshop,
            ModFileFactory modFileFactory,
            ILogger<ModlistPanel> logger) : 
            base(Resources.Mods, "mdi-toy-brick", false)
        {
            _steamApi = steamApi;
            _appFiles = appFiles;
            _uiConfig = uiConfig;
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
            RefreshModlistCommand = ReactiveCommand.Create(LoadModlist);
            UpdateModsCommand = ReactiveCommand.Create(() =>
            {
                UpdateMods(Modlist.OfType<IPublishedModFile>().Select(x => x.PublishedId).ToList());
            }, blocker.CanDownloadMods);

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
            Modlist.CollectionChanged += (_,_) =>
            {
                _profile.Modlist = Modlist.Select(x => x.Export()).ToList();
                _profile.SaveFile();
            };
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
                using (Modlist.SuspendNotifications())
                    await _modFileFactory.QueryFromWorkshop(Modlist);
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal(Resources.Error, tex.Message).OpenDialogueAsync();
            }
        }
        
        public async void AddModFromWorkshop(WorkshopSearchResult mod)
        {
            if (Modlist.Any(x => x is IPublishedModFile pub && pub.PublishedId == mod.PublishedFileId)) return;
            var file = await _modFileFactory.Create(mod);
            Modlist.Add(file);
        }

        public void RemoveModFile(IModFile mod)
        {
            Modlist.Remove(mod);
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
                await new ErrorModal(Resources.Error, tex.Message).OpenDialogueAsync();
            }

        }

        private async void FetchSteamCollection(UriBuilder builder)
        {
            var query = HttpUtility.ParseQueryString(builder.Query);
            var id = query.Get("id");
            if (id == null || !ulong.TryParse(id, out var collectionId))
            {
                await new ErrorModal(Resources.InvalidURL, Resources.InvalidURLText).OpenDialogueAsync();
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
                await new ErrorModal(Resources.Error, tex.Message).OpenDialogueAsync();
            }
        }

        private async void LoadModlist()
        {
            using (Modlist.SuspendNotifications())
            {
                Modlist.Clear();
                foreach (var mod in _profile.Modlist)
                    Modlist.Add(_modFileFactory.Create(mod));
                await _modFileFactory.QueryFromWorkshop(Modlist);
            }
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
                Title = "Add local mods",
                FileTypeFilter = [FileType.Pak],
                AllowMultiple = true
            });

            Modlist.AddRange(
                files.Where(f => f.Path.IsFile)
                    .Select(f => Path.GetFullPath(f.Path.LocalPath))
                    .Select(f => _modFileFactory.Create(f))
                );
        }

        private void OnExploreWorkshop()
        {
            if (_searchWindow != null) return;
            _searchWindow = new WorkshopSearch();
            _searchWindow.SearchViewModel = _workshop;
            _searchWindow.DataContext = _workshop;
            _searchWindow.Closing += OnSearchClosing;
            _searchWindow.Show();
        }

        private async void OnExportToJson()
        {
            var json = JsonSerializer.Serialize(new ModlistExport { Modlist = _profile.Modlist });
            await new ModlistTextImport(json, true, FileType.Json).OpenDialogueAsync();
        }

        private async void OnExportToTxt()
        {
            try
            {
                var content = string.Join("\r\n", _appFiles.Mods.GetResolvedModlist(_profile.Modlist));
                await new ModlistTextImport(content, true, FileType.Txt).OpenDialogueAsync();
            }
            catch
            {
                await new ErrorModal("Error",
                        "Some of the mods path cannot be resolved because the mod file was not found. " +
                        "In order to export your modlist, please unsure that all of the mods are not marked as missing.")
                    .OpenDialogueAsync();
            }
        }

        private async void OnFetchClicked()
        {
            if (string.IsNullOrEmpty(ModlistUrl)) return;

            var question = new QuestionModal("Replacement",
                "This action will replace your modlist, do you wish to continue ?");
            await question.OpenDialogueAsync();
            if (!question.Result) return;

            UriBuilder builder;
            try
            {
                builder = new UriBuilder(ModlistUrl);
            }
            catch
            {
                await new ErrorModal("Error", "Invalid URL.").OpenDialogueAsync();
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
                Title = "Import ModList",
                FileTypeFilter = [FileType.Json, FileType.Txt],
                SuggestedFileName = "modlist.txt"
            });

            if (files.Count <= 0) return;
            if (!files[0].Path.IsFile) return;
            var file = files[0].Path.LocalPath;
            var path = Path.GetFullPath(file);
            var ext = Path.GetExtension(file);
            if (ext == FileType.JsonExt)
                OnImportFromJsonFile(await File.ReadAllTextAsync(path));
            else if (ext == FileType.TxtExt)
                OnImportFromTxtFile(await File.ReadAllTextAsync(path));
            else
                await new ErrorModal("Wrong Type", "The type of file provided is unsupported.").OpenDialogueAsync();
        }

        private async void OnImportFromJsonFile(string json)
        {
            var modlist = JsonSerializer.Deserialize<ModlistExport>(json);
            if (modlist == null)
            {
                await new ErrorModal(Resources.InvalidJson, Resources.InvalidJsonText).OpenDialogueAsync();
                return;
            }

            _profile.Modlist = modlist.Modlist;
            _profile.SaveFile();
            LoadModlist();
        }

        private async void OnImportFromText()
        {
            var import = new ModlistTextImport(string.Empty, false, FileType.Json);
            await import.OpenDialogueAsync();

            if (import.Canceled) return;

            var text = import.Text;
            List<string>? modlist;
            try
            {
                var export = JsonSerializer.Deserialize<ModlistExport>(text);
                if (export == null)
                    throw new Exception("This is not Json.");
                modlist = export.Modlist;
            }
            catch
            {
                var split = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
                modlist = _appFiles.Mods.ParseModList(split).ToList();
            }

            if (import.Append)
                _profile.Modlist.AddRange(modlist);
            else
                _profile.Modlist = modlist;
            _profile.SaveFile();
            LoadModlist();
        }

        private void OnImportFromTxtFile(string text)
        {
            var split = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
            _profile.Modlist = _appFiles.Mods.ParseModList(split).ToList();
            _profile.SaveFile();
            LoadModlist();
        }

        private void OnModFileChanged(object sender, FileSystemEventArgs e)
        {
            var fullPath = e.FullPath;
            Dispatcher.UIThread.Invoke(() =>
            {
                if (!_appFiles.Mods.TryParseDirectory2ModID(fullPath, out var id)) return;
                for (var i = 0; i < Modlist.Count; i++)
                {
                    var modFile = Modlist[i];
                    if (modFile is not IPublishedModFile published || published.PublishedId != id) continue;
                    var path = published.PublishedId.ToString();
                    _appFiles.Mods.ResolveMod(ref path);
                    using(Modlist.SuspendNotifications())
                        Modlist[i] = _modFileFactory.Create(modFile, path);
                }
            });
        }

        private async void OnModlistCreate()
        {
            var modal = new InputTextModal(Resources.Create, Resources.ModlistName);
            await modal.OpenDialogueAsync();
            if (string.IsNullOrEmpty(modal.Text)) return;
            var name = modal.Text;
            if (Profiles.Contains(name))
            {
                await new ErrorModal(Resources.AlreadyExists, Resources.AlreadyExistsText).OpenDialogueAsync();
                return;
            }

            _appFiles.Mods.Create(name);
            RefreshProfiles();
            SelectedModlist = name;
        }

        private async void OnModlistDelete()
        {
            if (string.IsNullOrEmpty(SelectedModlist)) return;

            var question = new QuestionModal("Deletion",
                $"Do you wish to delete the selected modlist {SelectedModlist} ?");
            await question.OpenDialogueAsync();
            if (!question.Result) return;
            
            _appFiles.Mods.Delete(_profile.ProfileName);

            RefreshProfiles();
            SelectedModlist = string.Empty;
        }

        private async void OnModlistDuplicate()
        {
            var modal = new InputTextModal(Resources.Duplicate, Resources.ModlistName);
            modal.SetValue(SelectedModlist);
            await modal.OpenDialogueAsync();
            if (string.IsNullOrEmpty(modal.Text)) return;
            var name = modal.Text;
            if (Profiles.Contains(name))
            {
                await new ErrorModal(Resources.AlreadyExists, Resources.AlreadyExistsText).OpenDialogueAsync();
                return;
            }

            _profile = await _appFiles.Mods.Duplicate(_profile.ProfileName, name);
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