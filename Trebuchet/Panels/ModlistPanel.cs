#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Web;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using Trebuchet.Utils;
using Trebuchet.Windows;
using TrebuchetLib;
using TrebuchetUtils;
using TrebuchetUtils.Modals;

#endregion

namespace Trebuchet.Panels
{
    public class ModlistPanel : Panel,
        //TODO: Find Drag/Drop replacement
        //IDropTarget,
        IRecipient<SteamModlistReceived>
    {
        private readonly Config _config = StrongReferenceMessenger.Default.Send<ConfigRequest>();
        private TrulyObservableCollection<ModFile> _modlist = new();
        private string _modlistUrl = string.Empty;
        private FileSystemWatcher? _modWatcher;
        private bool _needRefresh;
        private ModListProfile _profile;
        private WorkshopSearch? _searchWindow;
        private string _selectedModlist = string.Empty;

        public ModlistPanel() : base("ModlistEditor")
        {
            LoadPanel();

            CreateModlistCommand = new SimpleCommand(OnModlistCreate);
            DeleteModlistCommand = new SimpleCommand(OnModlistDelete);
            DuplicateModlistCommand = new SimpleCommand(OnModlistDuplicate);
            ExploreLocalCommand = new SimpleCommand(OnExploreLocal);
            ExploreWorkshopCommand = new SimpleCommand(OnExploreWorkshop);
            ExportToJsonCommand = new SimpleCommand(OnExportToJson);
            ExportToTxtCommand = new SimpleCommand(OnExportToTxt);
            FetchCommand = new TaskBlockedCommand(OnFetchClicked, true, Operations.DownloadModlist);
            ImportFromFileCommand = new SimpleCommand(OnImportFromFile);
            ImportFromTextCommand = new SimpleCommand(OnImportFromText);
            RemoveModCommand = new SimpleCommand(OnModRemoved);
            OpenWorkshopCommand = new SimpleCommand(OnOpenWorkshop);
            UpdateModCommand = new TaskBlockedCommand(OnModUpdated, true, Operations.DownloadModlist);
            ModFilesDownloadCommand = new TaskBlockedCommand(OnModFilesDownload, true, Operations.SteamDownload,
                Operations.GameRunning, Operations.ServerRunning);
            RefreshModlistCommand = new TaskBlockedCommand(OnModlistRefresh, true, Operations.SteamPublishedFilesFetch);

            StrongReferenceMessenger.Default.Register<SteamModlistReceived>(this);
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

        //TODO: This seem unnecessary
        public IDataTemplate ItemTemplate
        {
            get
            {
                if (Application.Current == null) throw new Exception("Application.Current is null");

                if (Application.Current.Resources.TryGetResource("ModlistItems", Application.Current.ActualThemeVariant,
                        out var resource) && resource is IDataTemplate template)
                {
                    return template;
                }

                throw new Exception("Template ModlistItems not found");
            }
        }

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

        public SimpleCommand OpenWorkshopCommand { get; }

        public ObservableCollection<string> Profiles { get; set; } = new();

        public TaskBlockedCommand RefreshModlistCommand { get; }

        public SimpleCommand RemoveModCommand { get; }

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                OnSelectionChanged();
            }
        }

        public TaskBlockedCommand UpdateModCommand { get; }

        public void Receive(SteamModlistReceived message)
        {
            var update = from file in _modlist
                where file.IsPublished
                join details in message.Modlist on file.PublishedFileId equals details.PublishedFileID
                select new KeyValuePair<ModFile, PublishedFile>(file, details);

            List<ulong> updates =
                StrongReferenceMessenger.Default.Send(
                    new SteamModlistUpdateRequest(message.Modlist.GetManifestKeyValuePairs()));

            foreach (var u in update)
                u.Key.SetManifest(u.Value, updates.Contains(u.Value.PublishedFileID));
        }

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid;
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

        private void FetchJsonList(UriBuilder builder)
        {
            if (!GuiExtensions.Assert(
                    !StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.DownloadModlist)),
                    "Trebuchet is busy.")) return;

            new CatchedTasked(Operations.DownloadModlist, 15 * 1000)
                .Add(async cts =>
                {
                    var result = await Tools.DownloadModList(builder.ToString(), cts.Token);
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        _profile.Modlist = ModListProfile.ParseModList(result.Modlist).ToList();
                        _profile.SaveFile();
                        LoadModlist();
                    });
                }).Start();
        }

        private async void FetchSteamCollection(UriBuilder builder)
        {
            if (!GuiExtensions.Assert(
                    !StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.SteamCollectionFetch)),
                    "Trebuchet is busy.")) return;

            var query = HttpUtility.ParseQueryString(builder.Query);
            var id = query.Get("id");
            if (id == null || !ulong.TryParse(id, out var collectionId))
            {
                //TODO: Move into AppText
                await new ErrorModal("Invalid URL", "The steam URL seems to be missing its ID to be valid.").OpenDialogueAsync();
                return;
            }

            new CatchedTasked(Operations.SteamCollectionFetch, 15 * 1000)
                .Add(async cts =>
                {
                    var result =
                        await SteamRemoteStorage.GetCollectionDetails(new GetCollectionDetailsQuery(collectionId),
                            cts.Token);
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        var modlist = new List<string>();
                        foreach (var child in result.CollectionDetails.First().Children)
                            modlist.Add(child.PublishedFileId);
                        _profile.Modlist = modlist;
                        _profile.SaveFile();
                        LoadModlist();
                    });
                }).Start();
        }

        private void LoadManifests()
        {
            if (!GuiExtensions.Assert(
                    !StrongReferenceMessenger.Default.Send(
                        new OperationStateRequest(Operations.SteamPublishedFilesFetch)), "Trebuchet is busy.")) return;
            if (_modlist.Count == 0) return;

            StrongReferenceMessenger.Default.Send(new SteamModlistRequest(_selectedModlist));
        }

        private void LoadModlist()
        {
            _modlist.CollectionChanged -= OnModlistCollectionChanged;
            _modlist.Clear();

            foreach (var mod in _profile.Modlist)
            {
                var path = mod;
                _profile.ResolveMod(ref path);

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

            _selectedModlist = App.Config.CurrentModlistProfile;
            ModListProfile.ResolveProfile(_config, ref _selectedModlist);

            OnPropertyChanged(nameof(SelectedModlist));
            LoadProfile();
            RefreshProfiles();
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = ModListProfile.LoadProfile(_config, ModListProfile.GetPath(_config, _selectedModlist));
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
            _searchWindow.Closing += OnSearchClosing;
            _searchWindow.ModAdded += OnModAdded;
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
                var content = string.Join("\r\n", _profile.GetResolvedModlist());
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
            if (StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.DownloadModlist))) return;
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
                //TODO: Add to AppText
                await new ErrorModal("Invalid Json", "Loaded json could not be parsed.").OpenDialogueAsync();
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
                modlist = ModListProfile.ParseModList(split).ToList();
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
            _profile.Modlist = ModListProfile.ParseModList(split).ToList();
            _profile.SaveFile();
            LoadModlist();
        }

        private void OnModAdded(object? sender, WorkshopSearchResult mod)
        {
            if (_modlist.Any(x => x.IsPublished && x.PublishedFileId == mod.PublishedFileId)) return;
            var path = mod.PublishedFileId.ToString();
            _profile.ResolveMod(ref path);
            var file = new ModFile(mod, path);
            _modlist.Add(file);
            LoadManifests();
        }

        private void OnModFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!ModListProfile.TryParseDirectory2ModID(e.FullPath, out var id)) return;

            Dispatcher.UIThread.Invoke(() =>
            {
                foreach (var file in _modlist.Where(file => file.PublishedFileId == id))
                {
                    var path = file.PublishedFileId.ToString();
                    _profile.ResolveMod(ref path);
                    file.RefreshFile(path);
                }
            });
        }

        private void OnModFilesDownload(object? obj)
        {
            StrongReferenceMessenger.Default.Send(new ServerUpdateModsMessage(_profile.GetModIDList()));
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
            var modal = new InputTextModal("Create", "Modlist Name");
            await modal.OpenDialogueAsync();
            if (string.IsNullOrEmpty(modal.Text)) return;
            var name = modal.Text;
            if (Profiles.Contains(name))
            {
                //TODO: Add to AppText
                await new ErrorModal("Already Exists", "This mod list name is already used").OpenDialogueAsync();
                return;
            }

            _profile = ModListProfile.CreateProfile(_config, ModListProfile.GetPath(_config, name));
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
            ModListProfile.ResolveProfile(_config, ref profile);
            RefreshProfiles();
            _selectedModlist = profile;
            OnPropertyChanged(nameof(SelectedModlist));
        }

        private async void OnModlistDuplicate(object? obj)
        {
            var modal = new InputTextModal("Duplicate", "Modlist Name");
            modal.SetValue(_selectedModlist);
            await modal.OpenDialogueAsync();
            if (string.IsNullOrEmpty(modal.Text)) return;
            var name = modal.Text;
            if (Profiles.Contains(name))
            {
                //TODO: Add to AppText
                await new ErrorModal("Already Exists", "This mod list name is already used").OpenDialogueAsync();
                return;
            }

            var path = Path.Combine(ModListProfile.GetPath(_config, name));
            _profile.CopyFileTo(path);
            _profile = ModListProfile.LoadProfile(_config, path);
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

        private void OnModRemoved(object? obj)
        {
            if (obj is not ModFile modFile) return;

            _modlist.Remove(modFile);
        }

        private void OnModUpdated(object? obj)
        {
            if (obj is not ModFile modFile) return;

            StrongReferenceMessenger.Default.Send(new ServerUpdateModsMessage([modFile.PublishedFileId]));
        }

        private void OnOpenWorkshop(object? obj)
        {
            if (obj is not ModFile modFile) return;

            global::TrebuchetUtils.Utils.OpenWeb(string.Format(Config.SteamWorkshopURL, modFile.PublishedFileId));
        }

        private void OnSearchClosing(object? sender, CancelEventArgs e)
        {
            if (_searchWindow == null) return;
            _searchWindow.Closing -= OnSearchClosing;
            _searchWindow.ModAdded -= OnModAdded;
            _searchWindow = null;
        }

        private void OnSelectionChanged()
        {
            ModListProfile.ResolveProfile(_config, ref _selectedModlist);
            OnPropertyChanged(nameof(SelectedModlist));
            App.Config.CurrentModlistProfile = _selectedModlist;
            App.Config.SaveFile();
            LoadProfile();
        }

        private void RefreshModFileStatus()
        {
            foreach (var file in _modlist)
            {
                var path = file.PublishedFileId.ToString();
                _profile.ResolveMod(ref path);
                file.RefreshFile(path);
            }
        }

        private void RefreshProfiles()
        {
            Profiles = new ObservableCollection<string>(ModListProfile.ListProfiles(_config));
            OnPropertyChanged(nameof(Profiles));
        }

        private void SetupFileWatcher()
        {
            if (_modWatcher != null)
            {
                _modWatcher.Dispose();
                _modWatcher = null;
            }

            var path = Path.Combine(_config.ResolvedInstallPath, Config.FolderWorkshop);
            if (string.IsNullOrWhiteSpace(_config.ResolvedInstallPath) ||
                !Directory.Exists(_config.ResolvedInstallPath)) return;
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