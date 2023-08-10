using Goog;
using GoogGUI.Attributes;
using GoogLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogGUI
{
    [Panel(false, 0)]
    internal class ModlistHandler : IPanel
    {
        private bool _active = false;
        private SteamWorkWebAPI _api;
        private Config _config;
        private TrulyObservableCollection<ModFile> _modlist = new TrulyObservableCollection<ModFile>();
        private string _modlistURL = string.Empty;
        private Dictionary<string, SteamPublishedFile> _modManifests = new Dictionary<string, SteamPublishedFile>();
        private ModListProfile _profile = new ModListProfile();
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private WorkshopSearch? _searchWindow;
        private string _selectedModlist = string.Empty;
        private CancellationTokenSource? _source;

        public ModlistHandler(Config config)
        {
            _config = config;
            _config.FileSaved += OnConfigSaved;
            _api = new SteamWorkWebAPI(_config.SteamAPIKey);

            RefreshProfiles();
            SelectedModlist = _config.CurrentModlistProfile;
        }

        public event EventHandler? CanExecuteChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                OnPropertyChanged("Active");
            }
        }

        public ICommand CreateModlistCommand => new SimpleCommand(OnModlistCreate);

        public ICommand DeleteModlistCommand => new SimpleCommand(OnModlistDelete);

        public ICommand DownloadlistCommand => new SimpleCommand(OnModlistDownload);

        public ICommand DuplicateModlistCommand => new SimpleCommand(OnModlistDuplicate);

        public ICommand ExploreLocalCommand => new SimpleCommand(OnExploreLocal);

        public ICommand ExploreWorkshopCommand => new SimpleCommand(OnExploreWorkshop);

        public ICommand ExportToJsonCommand => new SimpleCommand(OnExportToJson);

        public ICommand ExportToTxtCommand => new SimpleCommand(OnExportToTxt);

        public ICommand FetchCommand => new SimpleCommand(OnFetchClicked);

        public ImageSource Icon => new BitmapImage(new Uri(@"/Icons/List.png", UriKind.Relative));

        public ICommand ImportFromFileCommand => new SimpleCommand(OnImportFromFile);

        public ICommand ImportFromTextCommand => new SimpleCommand(OnImportFromText);

        public ICommand ImportFromURLCommand => new SimpleCommand(OnExploreWorkshop);

        public bool IsLoading => _source != null;

        public object ItemTemplate => Application.Current.Resources["ModlistItems"];

        public string Label => "Mod List";

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

        public ICommand RefreshManifestCommand => new SimpleCommand(OnRefreshManifest);

        public ICommand RefreshModlistCommand => new SimpleCommand(OnModlistRefresh);

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                OnPropertyChanged("SelectedModlist");
                OnSelectionChanged();
            }
        }

        public DataTemplate Template => (DataTemplate)Application.Current.Resources["ModlistEditor"];

        public bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid && File.Exists(Path.Combine(_config.InstallPath, Config.FolderSteam, Config.FileSteamCMDBin));
        }

        public void Execute(object? parameter)
        {
            ((MainWindow)Application.Current.MainWindow).App.Panel = this;
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void FetchJsonList(UriBuilder builder)
        {
            _source = new CancellationTokenSource();
            OnPropertyChanged("IsLoading");
            Task.Run(() => ModListProfile.DownloadModList(builder.ToString(), _source.Token)).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnModListDownloaded(x)));
        }

        private void FetchSteamCollection(UriBuilder builder)
        {
            var query = HttpUtility.ParseQueryString(builder.Query);
            var id = query.Get("id");
            if (id == null || !long.TryParse(id, out _))
            {
                new ErrorModal("Invalid URL", "The steam URL seems to be missing its ID to be valid.").ShowDialog();
                return;
            }
            _source = new CancellationTokenSource();
            OnPropertyChanged("IsLoading");
            Task.Run(() => _api.GetCollectionDetails(new HashSet<string> { id }, _source.Token)).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnCollectionDownloaded(x)));
            return;
        }

        private bool GetFileInfo(string mod, out DateTime lastModified)
        {
            string path = mod;
            lastModified = default;
            if (!_config.ResolveMod(ref path)) return false;

            lastModified = File.GetLastWriteTimeUtc(path);
            return true;
        }

        private void LoadManifests()
        {
            if (_source != null) return;
            if (_modlist.Count == 0) return;

            _source = new CancellationTokenSource();
            OnPropertyChanged("IsLoading");
            HashSet<string> requested = new HashSet<string>();
            foreach (ModFile file in _modlist)
                if (file.IsID && !_modManifests.ContainsKey(file.Mod))
                    requested.Add(file.Mod);

            Task.Run(() => _api.GetPublishedFiles(requested, _source.Token)).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnManifestsLoaded(x)));
        }

        private void LoadModlist()
        {
            _modlist.CollectionChanged -= OnModlistCollectionChanged;
            _modlist.Clear();
            foreach (string m in _profile.Modlist)
            {
                bool exists = GetFileInfo(m, out DateTime lastModified);
                _modlist.Add(new ModFile(m, exists, lastModified));
            }
            _modlist.CollectionChanged += OnModlistCollectionChanged;
            OnPropertyChanged("Modlist");
            LoadManifests();
        }

        private void LoadModlistProfile()
        {
            if (string.IsNullOrEmpty(_selectedModlist)) return;
            string path = Path.Combine(_config.InstallPath, _config.VersionFolder, Config.FolderModlistProfiles, _selectedModlist + ".json");
            _profile = ModListProfile.LoadFile(path);
            _modlistURL = _profile.SyncURL;
            OnPropertyChanged("ModlistURL");

            LoadModlist();
        }

        private void OnCollectionDownloaded(Task<Dictionary<string, SteamCollectionDetails>> task)
        {
            _source?.Dispose();
            _source = null;
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

        private void OnConfigSaved(object? sender, Config e)
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
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
                var exists = GetFileInfo(path, out DateTime lastModified);
                _modlist.Add(new ModFile(path, exists, lastModified));
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
            string json = JsonSerializer.Serialize(_profile.Modlist);
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
            if (_source != null) return;
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
            List<string>? modlist = JsonSerializer.Deserialize<List<string>>(json);
            if (modlist == null)
            {
                new ErrorModal("Invalid Json", "Loaded json could not be parsed.");
                return;
            }

            _profile.Modlist = modlist;
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
                modlist = JsonSerializer.Deserialize<List<string>>(text);
                if (modlist == null)
                    throw new Exception("This is not Json");
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
            _source?.Dispose();
            _source = null;
            OnPropertyChanged("IsLoading");
            if (!task.IsCompletedSuccessfully)
            {
                if (task.Exception != null)
                    new ExceptionModal(task.Exception).ShowDialog();
                else
                    new ErrorModal("Modlist", "Could not download mod details of your modlist.", false).ShowDialog();
                return;
            }

            var toAdd = task.Result;
            foreach (var data in toAdd)
                _modManifests[data.Key] = data.Value;

            RefreshModData();
        }

        private void OnModAdded(object? sender, WorkshopSearchResult mod)
        {
            if (_modlist.Where((x) => x.IsID && x.Mod == mod.PublishedFile.publishedFileID).Any()) return;
            bool exists = GetFileInfo(mod.ModID, out DateTime lastModified);
            ModFile file = new ModFile(mod.PublishedFile.publishedFileID, exists, lastModified);
            _modlist.Add(file);
            LoadManifests();
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

            _profile = ModListProfile.CreateFile(Path.Combine(_config.InstallPath, _config.VersionFolder, Config.FolderModlistProfiles, name + ".json"));
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
                RefreshProfiles();
                SelectFirst();
            }
        }

        private void OnModlistDownload(object? obj)
        {
            if (_source != null) return;
            if (!App.TaskBlocker.IsAvailable) return;

            QuestionModal question = new QuestionModal("Download", "Do you wish to update your modlist ? This might take a while.");
            question.ShowDialog();
            if (question.Result != System.Windows.Forms.DialogResult.Yes) return;

            _source = new CancellationTokenSource();
            var task = Task.Run(() => Setup.UpdateMods(_config, SelectedModlist, _source.Token)).ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnModlistDownloaded(x)));
            App.TaskBlocker.Set(task, $"Updating your modlist mods {_selectedModlist}...", _source);
        }

        private void OnModlistDownloaded(Task<int> task)
        {
            _source?.Dispose();
            _source = null;
            App.TaskBlocker.Release();

            if (task.Exception != null)
            {
                new ExceptionModal(task.Exception).ShowDialog();
                return;
            }

            LoadModlist();
        }

        private void OnModListDownloaded(Task<List<string>> task)
        {
            _source?.Dispose();
            _source = null;
            OnPropertyChanged("IsLoading");
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

            string path = Path.Combine(_config.InstallPath, _config.VersionFolder, Config.FolderModlistProfiles, name + ".json");
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

        private void OnRefreshManifest(object? obj)
        {
            LoadManifests();
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
            _config.CurrentModlistProfile = _selectedModlist;
            _config.SaveFile();
            LoadModlistProfile();
        }

        private void RefreshModData()
        {
            foreach (ModFile file in _modlist)
                if (file.IsID && _modManifests.TryGetValue(file.Mod, out var value))
                    file.SetManifest(value);
        }

        private void RefreshProfiles()
        {
            _profiles.Clear();
            string folder = Path.Combine(_config.InstallPath, _config.VersionFolder, Config.FolderModlistProfiles);
            if (!Directory.Exists(folder))
                return;

            string[] profiles = Directory.GetFiles(folder, "*.json");
            foreach (string p in profiles)
                _profiles.Add(Path.GetFileNameWithoutExtension(p));
            OnPropertyChanged("Profiles");
        }

        private void SelectFirst()
        {
            string folder = Path.Combine(_config.InstallPath, _config.VersionFolder, Config.FolderModlistProfiles);
            if (!Directory.Exists(folder))
            {
                SelectedModlist = string.Empty;
                return;
            }

            string[] profiles = Directory.GetFiles(folder, "*.json");
            if (profiles.Length == 0)
            {
                SelectedModlist = string.Empty;
                return;
            }
            SelectedModlist = Path.GetFileNameWithoutExtension(profiles[0]);
        }
    }
}