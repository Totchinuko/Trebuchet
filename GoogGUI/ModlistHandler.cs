using Goog;
using GoogGUI.Attributes;
using GoogLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    [Panel("Mod Lists", "/Icons/List.png", false, 0, "ModlistEditor")]
    internal class ModlistHandler : Panel
    {
        private const string FetchManifests = "FetchManifests";
        private const string FetchModlist = "FetchModlist";
        private SteamWorkWebAPI _api;
        private TrulyObservableCollection<ModFile> _modlist = new TrulyObservableCollection<ModFile>();
        private string _modlistURL = string.Empty;
        private Dictionary<string, SteamPublishedFile> _modManifests = new Dictionary<string, SteamPublishedFile>();
        private FileSystemWatcher? _modWatcher;
        private ModListProfile _profile;
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private WorkshopSearch? _searchWindow;
        private string _selectedModlist = string.Empty;

        public ModlistHandler(Config config, UIConfig uiConfig) : base(config, uiConfig)
        {
            _config.FileSaved += OnConfigFileChanged;
            _api = new SteamWorkWebAPI(_config.SteamAPIKey);
            SetupFileWatcher();

            _selectedModlist = _uiConfig.CurrentModlistProfile;
            ModListProfile.ResolveProfile(_config, ref _selectedModlist);
            LoadProfile();
            RefreshProfiles();
        }

        public ICommand CreateModlistCommand => new SimpleCommand(OnModlistCreate);

        public ICommand DeleteModlistCommand => new SimpleCommand(OnModlistDelete);

        public ICommand DuplicateModlistCommand => new SimpleCommand(OnModlistDuplicate);

        public ICommand ExploreLocalCommand => new SimpleCommand(OnExploreLocal);

        public ICommand ExploreWorkshopCommand => new SimpleCommand(OnExploreWorkshop);

        public ICommand ExportToJsonCommand => new SimpleCommand(OnExportToJson);

        public ICommand ExportToTxtCommand => new SimpleCommand(OnExportToTxt);

        public ICommand FetchCommand => new TaskBlockedCommand(OnFetchClicked, FetchModlist);

        public ICommand ImportFromFileCommand => new SimpleCommand(OnImportFromFile);

        public ICommand ImportFromTextCommand => new SimpleCommand(OnImportFromText);

        public object ItemTemplate => Application.Current.Resources["ModlistItems"];

        public ICommand ModFilesDownloadCommand => new TaskBlockedCommand(OnModFilesDownload);

        public TrulyObservableCollection<ModFile> Modlist
        {
            get => _modlist;
            set
            {
                _modlist = value;
                OnModlistChanged();
            }
        }

        public string ModlistURL
        {
            get => _modlistURL;
            set
            {
                _modlistURL = value;
                OnModlistURLChanged();
            }
        }

        public ObservableCollection<string> Profiles { get => _profiles; set => _profiles = value; }

        public ICommand RefreshModlistCommand => new TaskBlockedCommand(OnModlistRefresh, FetchManifests);

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
            return _config.IsInstallPathValid && File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin));
        }

        private void FetchJsonList(UriBuilder builder)
        {
            if (App.TaskBlocker.IsSet(FetchModlist)) return;
            var token = App.TaskBlocker.Set(FetchModlist, 15 * 1000);
            Task.Run(() => ModListProfile.DownloadModList(builder.ToString(), token), token).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnModlistDownloaded(x)));
        }

        private void FetchSteamCollection(UriBuilder builder)
        {
            if (App.TaskBlocker.IsSet(FetchModlist)) return;

            var query = HttpUtility.ParseQueryString(builder.Query);
            var id = query.Get("id");
            if (id == null || !long.TryParse(id, out _))
            {
                new ErrorModal("Invalid URL", "The steam URL seems to be missing its ID to be valid.").ShowDialog();
                return;
            }
            var token = App.TaskBlocker.Set(FetchModlist, 15 * 1000);
            Task.Run(() => _api.GetCollectionDetails(new HashSet<string> { id }, token), token).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnCollectionDownloaded(x)));
        }

        private void LoadManifests()
        {
            if (App.TaskBlocker.IsSet(FetchManifests)) return;
            if (_modlist.Count == 0) return;

            var token = App.TaskBlocker.Set(FetchManifests, 15 * 1000);
            HashSet<string> requested = new HashSet<string>();
            foreach (ModFile file in _modlist)
                if (file.IsID && !_modManifests.ContainsKey(file.Mod))
                    requested.Add(file.Mod);

            Task.Run(() => _api.GetPublishedFiles(requested, token), token).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnManifestsLoaded(x)));
        }

        private void LoadModlist()
        {
            _modlist.CollectionChanged -= OnModlistCollectionChanged;
            _modlist.Clear();
            foreach (string m in _profile.Modlist)
            {
                string path = m;
                _config.ResolveMod(ref path);
                _modlist.Add(new ModFile(m, path));
            }
            _modlist.CollectionChanged += OnModlistCollectionChanged;
            OnPropertyChanged("Modlist");
            LoadManifests();
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = ModListProfile.LoadFile(ModListProfile.GetPath(_config, _selectedModlist));
            _modlistURL = _profile.SyncURL;
            OnPropertyChanged("ModlistURL");

            LoadModlist();
        }

        private void OnCollectionDownloaded(Task<Dictionary<string, SteamCollectionDetails>> task)
        {
            App.TaskBlocker.Release(FetchModlist);
            OnPropertyChanged("IsLoading");
            if (!task.IsCompleted || task.Exception != null)
            {
                new ErrorModal("Failed", $"Could not download the collection. ({(task.Exception?.Message ?? "Unknown Error")})").ShowDialog();
                return;
            }
            if (task.Result.Count == 0)
            {
                new ErrorModal("Not Found", "Collection could not be found.");
                return;
            }
            if (task.Result.First().Value.children.Length == 0)
                if (task.Result.Count == 0)
                {
                    new ErrorModal("Empty", "Collection is empty.");
                    return;
                }

            List<string> modlist = new List<string>();
            foreach (var child in task.Result.First().Value.children)
                modlist.Add(child.publishedFileId);
            _profile.Modlist = modlist;
            LoadModlist();
        }

        private void OnConfigFileChanged(object? sender, Config e)
        {
            SetupFileWatcher();
            OnCanExecuteChanged();
        }

        private void OnExploreLocal(object? obj)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.DefaultExt = FileType.Pak.extention;
            dialog.AddExtension = true;
            dialog.FileName = "ModArchive";
            dialog.Filter = FileType.Pak.Filter;
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            System.Windows.Forms.DialogResult result = dialog.ShowDialog(NativeWindow.GetIWin32Window(Application.Current.MainWindow));
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var path = Path.GetFullPath(dialog.FileName);
                _modlist.Add(new ModFile(path, path));
            }
        }

        private void OnExploreWorkshop(object? obj)
        {
            if (_searchWindow != null) return;
            _searchWindow = new WorkshopSearch(_api);
            _searchWindow.Closing += OnSearchClosing;
            _searchWindow.ModAdded += OnModAdded;
            _searchWindow.Show();
        }

        private void OnExportToJson(object? obj)
        {
            string json = JsonSerializer.Serialize(new ModlistExport { Modlist = _profile.Modlist });
            new ModlistTextImport(json, true, FileType.Json).ShowDialog();
        }

        private void OnExportToTxt(object? obj)
        {
            _config.ResolveModsPath(_profile.Modlist, out List<string> results, out List<string> error);
            if (error.Count > 0)
                new MessageModal("Invalid", "Some mods from your list are missing and could not be resolved").ShowDialog();
            new ModlistTextImport(string.Join("\r\n", results), true, FileType.Txt).ShowDialog();
        }

        private void OnFetchClicked(object? obj)
        {
            if (App.TaskBlocker.IsSet(FetchModlist)) return;
            if (string.IsNullOrEmpty(_modlistURL)) return;

            QuestionModal question = new QuestionModal("Replacement", "This action will replace your modlist, do you wish to continue ?");
            question.ShowDialog();
            if (question.Result != System.Windows.Forms.DialogResult.Yes) return;

            UriBuilder builder = new UriBuilder(_modlistURL);
            if (SteamWorkWebAPI.SteamHost == builder.Host)
                FetchSteamCollection(builder);
            else
                FetchJsonList(builder);
        }

        private void OnImportFromFile(object? obj)
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.DefaultExt = FileType.Json.extention;
            dialog.AddExtension = true;
            dialog.FileName = "Modlist";
            dialog.Filter = string.Join("|", new string[] { FileType.Json.Filter, FileType.Txt.Filter });
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            System.Windows.Forms.DialogResult result = dialog.ShowDialog(NativeWindow.GetIWin32Window(Application.Current.MainWindow));
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var path = Path.GetFullPath(dialog.FileName);
                var ext = Path.GetExtension(dialog.FileName);
                if (ext == FileType.Json.extention)
                    OnImportFromJsonFile(File.ReadAllText(path));
                else if (ext == FileType.Txt.extention)
                    OnImportFromTxtFile(File.ReadAllText(path));
                else
                    new ErrorModal("Wrong Type", "The type of file provided is unsupported.").ShowDialog();
            }
        }

        private void OnImportFromJsonFile(string json)
        {
            ModlistExport? modlist = JsonSerializer.Deserialize<ModlistExport>(json);
            if (modlist == null)
            {
                new ErrorModal("Invalid Json", "Loaded json could not be parsed.");
                return;
            }

            _profile.Modlist = modlist.Modlist;
            _profile.SaveFile();
            LoadModlist();
        }

        private void OnImportFromText(object? obj)
        {
            ModlistTextImport import = new ModlistTextImport(string.Empty, false, FileType.Json);
            import.ShowDialog();

            if (import.Canceled) return;

            string text = import.Text;
            List<string>? modlist;
            try
            {
                ModlistExport? export = JsonSerializer.Deserialize<ModlistExport>(text);
                if (export == null)
                    throw new Exception("This is not Json.");
                modlist = export.Modlist;
            }
            catch
            {
                modlist = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                ModListProfile.TryParseModList(ref modlist);
            }

            if (modlist == null)
            {
                new ErrorModal("Invalid Modlist", "The modlist could not be parsed.");
                return;
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
            List<string> modlist = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            ModListProfile.TryParseModList(ref modlist);
            _profile.Modlist = modlist;
            _profile.SaveFile();
            LoadModlist();
        }

        private void OnManifestsLoaded(Task<Dictionary<string, SteamPublishedFile>> task)
        {
            App.TaskBlocker.Release(FetchManifests);
            OnPropertyChanged("IsLoading");
            if (!task.IsCompletedSuccessfully)
            {
                if (task.Exception != null)
                    new ExceptionModal(task.Exception).ShowDialog();
                else
                    new ErrorModal("Modlist", "Could not download mod details of your modlist.", false).ShowDialog();
                return;
            }

            foreach (var data in task.Result)
                _modManifests[data.Key] = data.Value;

            foreach (ModFile file in _modlist)
                if (file.IsID && _modManifests.TryGetValue(file.Mod, out var value))
                    file.SetManifest(value);
        }

        private void OnModAdded(object? sender, WorkshopSearchResult mod)
        {
            if (_modlist.Where((x) => x.IsID && x.Mod == mod.PublishedFile.publishedFileID).Any()) return;
            string path = mod.ModID;
            _config.ResolveMod(ref path);
            ModFile file = new ModFile(mod.PublishedFile.publishedFileID, path);
            _modlist.Add(file);
            LoadManifests();
        }

        private void OnModFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!ModListProfile.TryParseDirectory2ModID(e.FullPath, out string id)) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var file in _modlist.Where(file => file.Mod == id))
                {
                    string path = file.Mod;
                    _config.ResolveMod(ref path);
                    file.RefreshFile(path);
                }
            });
        }

        private void OnModFilesDownload(object? obj)
        {
            if (!App.TaskBlocker.IsAvailable) return;

            QuestionModal question = new QuestionModal("Download", "Do you wish to update your modlist ? This might take a while.");
            question.ShowDialog();
            if (question.Result != System.Windows.Forms.DialogResult.Yes) return;

            var token = App.TaskBlocker.SetMain($"Updating your modlist mods {_selectedModlist}...");
            var task = Task.Run(() => Setup.UpdateMods(_config, SelectedModlist, token), token).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnModFilesDownloaded(x)));
        }

        private void OnModFilesDownloaded(Task<int> task)
        {
            App.TaskBlocker.ReleaseMain();
            if (task.Exception != null)
            {
                new ExceptionModal(task.Exception).ShowDialog();
                return;
            }

            RefreshModFileStatus();
        }

        private void OnModlistChanged()
        {
            _profile.Modlist.Clear();
            foreach (ModFile file in _modlist)
                _profile.Modlist.Add(file.Mod);
            _profile.SaveFile();
        }

        private void OnModlistCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnModlistChanged();
        }

        private void OnModlistCreate(object? obj)
        {
            ChooseNameModal modal = new ChooseNameModal("Create", string.Empty);
            modal.ShowDialog();
            string name = modal.Name;
            if (string.IsNullOrEmpty(name)) return;
            if (_profiles.Contains(name))
            {
                new ErrorModal("Already Exitsts", "This mod list name is already used").ShowDialog();
                return;
            }

            _profile = ModListProfile.CreateFile(ModListProfile.GetPath(_config, name));
            _profile.SaveFile();
            RefreshProfiles();
            SelectedModlist = name;
        }

        private void OnModlistDelete(object? obj)
        {
            if (string.IsNullOrEmpty(_selectedModlist)) return;
            if (_profile == null) return;

            QuestionModal question = new QuestionModal("Deletion", $"Do you wish to delete the selected modlist {_selectedModlist} ?");
            question.ShowDialog();
            if (question.Result == System.Windows.Forms.DialogResult.Yes)
            {
                _profile.DeleteFile();

                string profile = string.Empty;
                ModListProfile.ResolveProfile(_config, ref profile);
                RefreshProfiles();
                SelectedModlist = profile;
            }
        }

        private void OnModlistDownloaded(Task<List<string>> task)
        {
            App.TaskBlocker.Release(FetchModlist);
            if (!task.IsCompleted || task.Exception != null)
            {
                new ErrorModal("Failed", $"Could not download the file. ({(task.Exception?.Message ?? "Unknown Error")})").ShowDialog();
                return;
            }
            if (task.Result.Count == 0)
            {
                new ErrorModal("Empty", "Downloaded list is empty.");
                return;
            }

            List<string> modlist = task.Result;
            ModListProfile.TryParseModList(ref modlist);
            _profile.Modlist = modlist;
            LoadModlist();
        }

        private void OnModlistDuplicate(object? obj)
        {
            ChooseNameModal modal = new ChooseNameModal("Duplicate", _selectedModlist);
            modal.ShowDialog();
            string name = modal.Name;
            if (string.IsNullOrEmpty(name)) return;
            if (_profiles.Contains(name))
            {
                new ErrorModal("Already Exitsts", "This mod list name is already used").ShowDialog();
                return;
            }

            string path = Path.Combine(ModListProfile.GetPath(_config, name));
            _profile.CopyFileTo(path);
            _profile = ModListProfile.LoadFile(path);
            _profile.SaveFile();
            RefreshProfiles();
            SelectedModlist = name;
        }

        private void OnModlistRefresh(object? obj)
        {
            _modManifests.Clear();
            LoadModlist();
        }

        private void OnModlistURLChanged()
        {
            _profile.SyncURL = _modlistURL;
            _profile.SaveFile();
            OnPropertyChanged("ModlistURL");
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
            OnPropertyChanged("SelectedModlist");
            _uiConfig.CurrentModlistProfile = _selectedModlist;
            _config.SaveFile();
            LoadProfile();
        }

        private void RefreshModFileStatus()
        {
            foreach (ModFile file in _modlist)
            {
                string path = file.Mod;
                _config.ResolveMod(ref path);
                file.RefreshFile(path);
            }
        }

        private void RefreshProfiles()
        {
            _profiles = new ObservableCollection<string>(ModListProfile.ListProfiles(_config));
            OnPropertyChanged("Profiles");
        }

        private void SetupFileWatcher()
        {
            if (_modWatcher != null) return;
            string path = Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FolderSteamMods, _config.ClientAppID);
            if (!Directory.Exists(path)) return;
                
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
            //  watcher.Filter = "*.pak";
            _modWatcher.IncludeSubdirectories = false;
            _modWatcher.EnableRaisingEvents = true;
        }
    }
}