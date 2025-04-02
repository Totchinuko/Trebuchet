using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
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
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet.ViewModels.Panels
{
    public class ModlistPanel : Panel
    {
        private readonly SteamAPI _steamApi;
        private readonly AppFiles _appFiles;
        private readonly UIConfig _uiConfig;
        private readonly WorkshopSearchViewModel _workshop;
        private readonly ILogger<ModlistPanel> _logger;
        private TrulyObservableCollection<ModFile> _modlist = new();
        private string _modlistUrl = string.Empty;
        private FileSystemWatcher? _modWatcher;
        private bool _needRefresh;
        private ModListProfile _profile;
        private WorkshopSearch? _searchWindow;
        private string _selectedModlist = string.Empty;

        public ModlistPanel(
            ITinyMessengerHub messenger,
            SteamAPI steamApi, 
            AppFiles appFiles,
            UIConfig uiConfig, 
            WorkshopSearchViewModel workshop,
            ILogger<ModlistPanel> logger) : 
            base(Resources.Mods, "mdi-toy-brick", false)
        {
            _steamApi = steamApi;
            _appFiles = appFiles;
            _uiConfig = uiConfig;
            _workshop = workshop;
            _logger = logger;
            _workshop.ModAdded += (_,mod) => AddModFromWorkshop(mod);
            LoadPanel();

            CreateModlistCommand = new SimpleCommand().Subscribe(OnModlistCreate);
            DeleteModlistCommand = new SimpleCommand().Subscribe(OnModlistDelete);
            DuplicateModlistCommand = new SimpleCommand().Subscribe(OnModlistDuplicate);
            ExploreLocalCommand = new SimpleCommand().Subscribe(OnExploreLocal);
            ExploreWorkshopCommand = new SimpleCommand().Subscribe(OnExploreWorkshop);
            ExportToJsonCommand = new SimpleCommand().Subscribe(OnExportToJson);
            ExportToTxtCommand = new SimpleCommand().Subscribe(OnExportToTxt);
            FetchCommand = new TaskBlockedCommand()
                .SetBlockingType<DownloadModlist>()
                .Subscribe(OnFetchClicked);
            ImportFromFileCommand = new SimpleCommand().Subscribe(OnImportFromFile);
            ImportFromTextCommand = new SimpleCommand().Subscribe(OnImportFromText);
            ModFilesDownloadCommand = new TaskBlockedCommand()
                .SetBlockingType<SteamDownload>()
                .SetBlockingType<ClientRunning>()
                .SetBlockingType<ServersRunning>()
                .Subscribe(OnModFilesDownload);
            RefreshModlistCommand = new TaskBlockedCommand()
                .SetBlockingType<DownloadModlist>()
                .Subscribe(OnModlistRefresh);
        }

        public SimpleCommand CreateModlistCommand { get; }

        public SimpleCommand DeleteModlistCommand { get; }

        public SimpleCommand DuplicateModlistCommand { get; }

        public SimpleCommand ExploreLocalCommand { get; }

        public SimpleCommand ExploreWorkshopCommand { get; }

        public SimpleCommand ExportToJsonCommand { get; }

        public SimpleCommand ExportToTxtCommand { get; }

        public SimpleCommand FetchCommand { get; }

        public SimpleCommand ImportFromFileCommand { get; }

        public SimpleCommand ImportFromTextCommand { get; }

        public SimpleCommand ModFilesDownloadCommand { get; }

        public TrulyObservableCollection<ModFile> Modlist
        {
            get => _modlist;
            set
            {
                _modlist = value;
                OnModlistChanged();
            }
        }

        public string ModlistUrl
        {
            get => _modlistUrl;
            set
            {
                _modlistUrl = value;
                OnModlistURLChanged();
            }
        }

        public ObservableCollection<string> Profiles { get; set; } = new();

        public SimpleCommand RefreshModlistCommand { get; }

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                OnSelectionChanged();
            }
        }

        public override bool CanExecute(object? parameter)
        {
            return true;
        }

        public override void Execute(object? parameter)
        {
            base.Execute(parameter);
            if (_needRefresh)
            {
                _needRefresh = false;
                LoadPanel();
            }
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            _needRefresh = true;
        }
        
        public async void UpdateMods(List<ulong> mods)
        {
            try
            {
                await _steamApi.UpdateMods(mods);
                await LoadManifests();
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal(Resources.Error, tex.Message).OpenDialogueAsync();
            }
        }
        
        public async void AddModFromWorkshop(WorkshopSearchResult mod)
        {
            if (_modlist.Any(x => x.IsPublished && x.PublishedFileId == mod.PublishedFileId)) return;
            var path = mod.PublishedFileId.ToString();
            _appFiles.Mods.ResolveMod(ref path);
            var file = new ModFile(mod, path);
            file.RemoveModCommand.Subscribe(OnRemoveMod);
            file.OpenModPageCommand.Subscribe(OnOpenSteamPage);
            file.UpdateModCommand.Subscribe(OnUpdateMod);
            _modlist.Add(file);
            await LoadManifests();
        }

        private void OnOpenSteamPage(object? parameter)
        {
            if(parameter is ModFile file)
                TrebuchetUtils.Utils.OpenWeb(string.Format(Constants.SteamWorkshopURL, file.PublishedFileId));
        }

        private void OnRemoveMod(object? parameter)
        {
            if(parameter is ModFile file)
                _modlist.Remove(file);
        }

        private void OnUpdateMod(object? parameter)
        {
            if(parameter is ModFile file)
                UpdateMods([file.PublishedFileId]);
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

        private async Task LoadManifests()
        {
            try
            {
                var response = await _steamApi.RequestModDetails(_profile.GetWorkshopMods().ToList());
                var neededUpdate = _steamApi.CheckModsForUpdate(response.GetManifestKeyValuePairs().ToList());
                foreach (var modFile in Modlist)
                {
                    var pubFile = response.FirstOrDefault(x => x.PublishedFileID == modFile.PublishedFileId);
                    if (pubFile == null) continue;
                    modFile.SetManifest(pubFile, neededUpdate.Contains(modFile.PublishedFileId));
                }
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal(Resources.Error, tex.Message).OpenDialogueAsync();
            }
        }

        private async void LoadModlist()
        {
            _modlist.CollectionChanged -= OnModlistCollectionChanged;
            _modlist.Clear();

            foreach (var mod in _profile.Modlist)
            {
                var path = mod;
                _appFiles.Mods.ResolveMod(ref path);
                ModFile modFile;
                if (ulong.TryParse(mod, out var publishedFileId))
                    modFile = new (publishedFileId, path);
                else
                    modFile = new(path);
                modFile.RemoveModCommand.Subscribe(OnRemoveMod);
                modFile.OpenModPageCommand.Subscribe(OnOpenSteamPage);
                modFile.UpdateModCommand.Subscribe(OnUpdateMod);
                _modlist.Add(modFile);
            }

            _modlist.CollectionChanged += OnModlistCollectionChanged;
            OnPropertyChanged(nameof(Modlist));
            await LoadManifests();
        }

        [MemberNotNull("_profile")]
        private void LoadPanel()
        {
            SetupFileWatcher();

            _selectedModlist = _uiConfig.CurrentModlistProfile;
            _selectedModlist = _appFiles.Mods.ResolveProfile(_selectedModlist);

            OnPropertyChanged(nameof(SelectedModlist));
            LoadProfile();
            RefreshProfiles();
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = _appFiles.Mods.Get(_selectedModlist);
            _modlistUrl = _profile.SyncURL;
            OnPropertyChanged(nameof(ModlistUrl));

            LoadModlist();
        }

        private async void OnExploreLocal(object? obj)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (desktop.MainWindow == null) return;

            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Add local mods",
                FileTypeFilter = [FileType.Pak],
                AllowMultiple = true
            });

            foreach (var file in files)
            {
                if(!file.Path.IsFile) continue; 
                var path = Path.GetFullPath(file.Path.LocalPath);
                var modFile = new ModFile(path);
                modFile.RemoveModCommand.Subscribe(OnRemoveMod);
                modFile.OpenModPageCommand.Subscribe(OnOpenSteamPage);
                modFile.UpdateModCommand.Subscribe(OnUpdateMod);
                _modlist.Add(modFile);
            }
        }

        private void OnExploreWorkshop(object? obj)
        {
            if (_searchWindow != null) return;
            _searchWindow = new WorkshopSearch();
            _searchWindow.SearchViewModel = _workshop;
            _searchWindow.DataContext = _workshop;
            _searchWindow.Closing += OnSearchClosing;
            _searchWindow.Show();
        }

        private async void OnExportToJson(object? obj)
        {
            var json = JsonSerializer.Serialize(new ModlistExport { Modlist = _profile.Modlist });
            await new ModlistTextImport(json, true, FileType.Json).OpenDialogueAsync();
        }

        private async void OnExportToTxt(object? obj)
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

        private async void OnFetchClicked(object? obj)
        {
            if (string.IsNullOrEmpty(_modlistUrl)) return;

            var question = new QuestionModal("Replacement",
                "This action will replace your modlist, do you wish to continue ?");
            await question.OpenDialogueAsync();
            if (!question.Result) return;

            UriBuilder builder;
            try
            {
                builder = new UriBuilder(_modlistUrl);
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

        private async void OnImportFromFile(object? obj)
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

        private async void OnImportFromText(object? obj)
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
                foreach (var file in _modlist.Where(file => file.PublishedFileId == id))
                {
                    var path = file.PublishedFileId.ToString();
                    _appFiles.Mods.ResolveMod(ref path);
                    file.RefreshFile(path);
                }
            });
        }

        private void OnModFilesDownload(object? obj)
        {
            UpdateMods(_profile.GetWorkshopMods().ToList());
        }

        private void OnModlistChanged()
        {
            _profile.Modlist.Clear();
            _profile.Modlist.AddRange(
                _modlist.Select(file => file.ToString())
            );
            _profile.SaveFile();
        }

        private void OnModlistCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnModlistChanged();
        }

        private async void OnModlistCreate(object? obj)
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

            _profile = _appFiles.Mods.Create(name);
            RefreshProfiles();
            SelectedModlist = name;
        }

        private async void OnModlistDelete(object? obj)
        {
            if (string.IsNullOrEmpty(_selectedModlist)) return;

            var question = new QuestionModal("Deletion",
                $"Do you wish to delete the selected modlist {_selectedModlist} ?");
            await question.OpenDialogueAsync();
            if (!question.Result) return;
            
            _appFiles.Mods.Delete(_profile.ProfileName);

            var profile = string.Empty;
            profile = _appFiles.Mods.ResolveProfile(profile);
            RefreshProfiles();
            _selectedModlist = profile;
            OnPropertyChanged(nameof(SelectedModlist));
        }

        private async void OnModlistDuplicate(object? obj)
        {
            var modal = new InputTextModal(Resources.Duplicate, Resources.ModlistName);
            modal.SetValue(_selectedModlist);
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

        private void OnModlistRefresh(object? obj)
        {
            LoadModlist();
        }

        private void OnModlistURLChanged()
        {
            _profile.SyncURL = _modlistUrl;
            _profile.SaveFile();
            OnPropertyChanged(nameof(ModlistUrl));
        }

        private void OnSearchClosing(object? sender, CancelEventArgs e)
        {
            if (_searchWindow == null) return;
            _searchWindow.Closing -= OnSearchClosing;
            _searchWindow = null;
        }

        private void OnSelectionChanged()
        {
            _selectedModlist = _appFiles.Mods.ResolveProfile(_selectedModlist);
            OnPropertyChanged(nameof(SelectedModlist));
            _uiConfig.CurrentModlistProfile = _selectedModlist;
            _uiConfig.SaveFile();
            LoadProfile();
        }

        private void RefreshProfiles()
        {
            Profiles = new ObservableCollection<string>(_appFiles.Mods.ListProfiles());
            OnPropertyChanged(nameof(Profiles));
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