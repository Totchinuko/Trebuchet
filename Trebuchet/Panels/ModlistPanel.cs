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
using System.Web;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using Trebuchet.Messages;
using Trebuchet.Services;
using Trebuchet.Services.TaskBlocker;
using Trebuchet.Utils;
using Trebuchet.ViewModels;
using Trebuchet.Windows;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

namespace Trebuchet.Panels
{
    public class ModlistPanel : Panel, ITinyRecipient<ModListMessages>
    {
        private readonly SteamAPI _steamApi;
        private readonly AppSetup _setup;
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
            SteamAPI steamApi, 
            AppSetup setup,
            AppFiles appFiles,
            UIConfig uiConfig, 
            WorkshopSearchViewModel workshop,
            ILogger<ModlistPanel> logger) : 
            base("Mods", "ModlistEditor", "mdi-toy-brick", false)
        {
            _steamApi = steamApi;
            _setup = setup;
            _appFiles = appFiles;
            _uiConfig = uiConfig;
            _workshop = workshop;
            _logger = logger;
            LoadPanel();

            CreateModlistCommand = new SimpleCommand(OnModlistCreate);
            DeleteModlistCommand = new SimpleCommand(OnModlistDelete);
            DuplicateModlistCommand = new SimpleCommand(OnModlistDuplicate);
            ExploreLocalCommand = new SimpleCommand(OnExploreLocal);
            ExploreWorkshopCommand = new SimpleCommand(OnExploreWorkshop);
            ExportToJsonCommand = new SimpleCommand(OnExportToJson);
            ExportToTxtCommand = new SimpleCommand(OnExportToTxt);
            FetchCommand = new TaskBlockedCommand(OnFetchClicked)
                .SetBlockingType<DownloadModlist>();
            ImportFromFileCommand = new SimpleCommand(OnImportFromFile);
            ImportFromTextCommand = new SimpleCommand(OnImportFromText);
            ModFilesDownloadCommand = new TaskBlockedCommand(OnModFilesDownload)
                .SetBlockingType<SteamDownload>()
                .SetBlockingType<ClientRunning>()
                .SetBlockingType<ServersRunning>();
            RefreshModlistCommand = new TaskBlockedCommand(OnModlistRefresh)
                .SetBlockingType<DownloadModlist>();

            TinyMessengerHub.Default.Subscribe(this);
        }

        public SimpleCommand CreateModlistCommand { get; }

        public SimpleCommand DeleteModlistCommand { get; }

        public SimpleCommand DuplicateModlistCommand { get; }

        public SimpleCommand ExploreLocalCommand { get; }

        public SimpleCommand ExploreWorkshopCommand { get; }

        public SimpleCommand ExportToJsonCommand { get; }

        public SimpleCommand ExportToTxtCommand { get; }

        public TaskBlockedCommand FetchCommand { get; }

        public SimpleCommand ImportFromFileCommand { get; }

        public SimpleCommand ImportFromTextCommand { get; }

        public TaskBlockedCommand ModFilesDownloadCommand { get; }

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

        public TaskBlockedCommand RefreshModlistCommand { get; }

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
            return _setup.Config.IsInstallPathValid;
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
        
        public void Receive(ModListMessages message)
        {
            if(message is ModListOpenModSteamMessage openSteam)
                TrebuchetUtils.Utils.OpenWeb(string.Format(Constants.SteamWorkshopURL, openSteam.ModFile.PublishedFileId));
            else if(message is ModListRemoveModMessage removeMod)
                _modlist.Remove(removeMod.ModFile);
            else if(message is ModListUpdateModMessage updateMod)
                UpdateMods([updateMod.ModFile.PublishedFileId]);
        }

        public async void UpdateMods(List<ulong> mods)
        {
            try
            {
                await _steamApi.UpdateMods(mods);
            }
            catch (TrebException tex)
            {
                _logger.LogError(tex.Message);
                await new ErrorModal(App.GetAppText("Error"), tex.Message).OpenDialogueAsync();
            }
        }
        
        public void AddModFromWorkshop(WorkshopSearchResult mod)
        {
            if (_modlist.Any(x => x.IsPublished && x.PublishedFileId == mod.PublishedFileId)) return;
            var path = mod.PublishedFileId.ToString();
            _appFiles.Mods.ResolveMod(ref path);
            var file = new ModFile(mod, path);
            _modlist.Add(file);
            LoadManifests();
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
                await new ErrorModal(App.GetAppText("Error"), tex.Message).OpenDialogueAsync();
            }

        }

        private async void FetchSteamCollection(UriBuilder builder)
        {
            var query = HttpUtility.ParseQueryString(builder.Query);
            var id = query.Get("id");
            if (id == null || !ulong.TryParse(id, out var collectionId))
            {
                await new ErrorModal(App.GetAppText("InvalidURL"), App.GetAppText("InvalidURL_Message")).OpenDialogueAsync();
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
                await new ErrorModal(App.GetAppText("Error"), tex.Message).OpenDialogueAsync();
            }
        }

        private async void LoadManifests()
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
                await new ErrorModal(App.GetAppText("Error"), tex.Message).OpenDialogueAsync();
            }
        }

        private void LoadModlist()
        {
            _modlist.CollectionChanged -= OnModlistCollectionChanged;
            _modlist.Clear();

            foreach (var mod in _profile.Modlist)
            {
                var path = mod;
                _appFiles.Mods.ResolveMod(ref path);

                _modlist.Add(ulong.TryParse(mod, out var publishedFileId)
                    ? new ModFile(publishedFileId, path)
                    : new ModFile(path));
            }

            _modlist.CollectionChanged += OnModlistCollectionChanged;
            OnPropertyChanged(nameof(Modlist));
            LoadManifests();
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
            _profile = ModListProfile.LoadProfile(_appFiles.Mods.GetPath(_selectedModlist));
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
                _modlist.Add(new ModFile(path));
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
                await new ErrorModal(App.GetAppText("InvalidJson"), App.GetAppText("InvalidJson_Message")).OpenDialogueAsync();
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
            if (!_appFiles.Mods.TryParseDirectory2ModID(e.FullPath, out var id)) return;

            Dispatcher.UIThread.Invoke(() =>
            {
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
            var modal = new InputTextModal(App.GetAppText("Create"), App.GetAppText("ModlistName"));
            await modal.OpenDialogueAsync();
            if (string.IsNullOrEmpty(modal.Text)) return;
            var name = modal.Text;
            if (Profiles.Contains(name))
            {
                await new ErrorModal(App.GetAppText("AlreadyExists"), App.GetAppText("AlreadyExists_Message")).OpenDialogueAsync();
                return;
            }

            _profile = ModListProfile.CreateProfile(_appFiles.Mods.GetPath(name));
            _profile.SaveFile();
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
            
            _profile.DeleteFile();

            var profile = string.Empty;
            profile = _appFiles.Mods.ResolveProfile(profile);
            RefreshProfiles();
            _selectedModlist = profile;
            OnPropertyChanged(nameof(SelectedModlist));
        }

        private async void OnModlistDuplicate(object? obj)
        {
            var modal = new InputTextModal(App.GetAppText("Duplicate"), App.GetAppText("ModlistName"));
            modal.SetValue(_selectedModlist);
            await modal.OpenDialogueAsync();
            if (string.IsNullOrEmpty(modal.Text)) return;
            var name = modal.Text;
            if (Profiles.Contains(name))
            {
                await new ErrorModal(App.GetAppText("AlreadyExists"), App.GetAppText("AlreadyExists_Message")).OpenDialogueAsync();
                return;
            }

            var path = Path.Combine(_appFiles.Mods.GetPath(name));
            _profile.CopyFileTo(path);
            _profile = ModListProfile.LoadProfile(path);
            _profile.SaveFile();
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

        private void RefreshModFileStatus()
        {
            foreach (var file in _modlist)
            {
                var path = file.PublishedFileId.ToString();
                _appFiles.Mods.ResolveMod(ref path);
                file.RefreshFile(path);
            }
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

            var path = Path.Combine(_setup.Config.ResolvedInstallPath(), Constants.FolderWorkshop);
            if (string.IsNullOrWhiteSpace(_setup.Config.ResolvedInstallPath()) ||
                !Directory.Exists(_setup.Config.ResolvedInstallPath())) return;
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