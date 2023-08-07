using Goog;
using GoogLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    internal class ModlistHandler : INotifyPropertyChanged
    {
        private SteamWorkWebAPI _api;
        private Config _config;
        private TrulyObservableCollection<ModFile> _modlist = new TrulyObservableCollection<ModFile>();
        private Dictionary<string, SteamPublishedFile> _modManifests = new Dictionary<string, SteamPublishedFile>();
        private ModListProfile _profile = new ModListProfile();
        private string _selectedModlist = string.Empty;
        private CancellationTokenSource? _source;
        private WorkshopSearch? _searchWindow;

        public ModlistHandler(Config config)
        {
            RefreshManifestCommand = new SimpleCommand(OnRefreshManifest);
            ImportFromFileCommand = new SimpleCommand(OnImportFromFile);
            ImportFromTextCommand = new SimpleCommand(OnImportFromText);
            ImportFromURLCommand = new SimpleCommand(OnExploreWorkshop);
            ExploreWorkshopCommand = new SimpleCommand(OnExploreWorkshop);

            _config = config;
            _api = new SteamWorkWebAPI(_config.SteamAPIKey);

            _selectedModlist = _config.CurrentModlistProfile;
            LoadModlist();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand ExploreWorkshopCommand { get; private set; }

        public ICommand ImportFromFileCommand { get; private set; }

        public ICommand ImportFromTextCommand { get; private set; }

        public ICommand ImportFromURLCommand { get; private set; }

        public bool IsLoading => _source != null;

        public object ItemTemplate => Application.Current.Resources["ModlistItems"];

        public TrulyObservableCollection<ModFile> Modlist
        {
            get => _modlist;
            set
            {
                _modlist = value;
                OnModlistChanged();
            }
        }

        public ICommand RefreshManifestCommand { get; private set; }

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                OnSelectionChanged();
            }
        }

        public object Template => Application.Current.Resources["ModlistEditor"];

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void LoadManifests()
        {
            if (_source != null) return;

            _source = new CancellationTokenSource();
            OnPropertyChanged("IsLoading");
            List<string> requested = new List<string>();
            foreach (ModFile file in _modlist)
                if (file.IsID && !_modManifests.ContainsKey(file.Mod))
                    requested.Add(file.Mod);

            Task.Run(() => _api.GetPublishedFiles(requested, _source.Token)).ContinueWith(OnManifestsLoaded);
        }

        private void LoadModAuthors()
        {
            if (_source != null) return;

            _source = new CancellationTokenSource();
            HashSet<string> requested = new HashSet<string>();
            foreach (ModFile file in _modlist)
                if (file.PublishedFile != null && !string.IsNullOrEmpty(file.AuthorName))
                    requested.Add(file.PublishedFile.creator);

            Task.Run(() => _api.ExtractUserNames(requested.ToList(), _source.Token)).ContinueWith(OnAuthorsLoaded);
        }

        private void LoadModlist()
        {
            if (string.IsNullOrEmpty(_selectedModlist)) return;
            string path = Path.Combine(_config.InstallPath, _config.VersionFolder, Config.FolderModlistProfiles, _selectedModlist + ".json");
            _profile = Tools.LoadFile<ModListProfile>(path);

            _modlist.Clear();
            foreach (string m in _profile.Modlist)
                _modlist.Add(new ModFile(m));
            OnPropertyChanged("Modlist");
            LoadManifests();
        }

        private void OnAuthorsLoaded(Task<Dictionary<string, string>> task)
        {
            _source?.Dispose();
            _source = null;
            Application.Current.Dispatcher.Invoke(() => OnPropertyChanged("IsLoading"));
            if (!task.IsCompletedSuccessfully)
            {
                if (task.Exception != null)
                    Application.Current.Dispatcher.Invoke(() => new ExceptionModal(task.Exception).ShowDialog());
                else
                    Application.Current.Dispatcher.Invoke(() => new ErrorModal("Modlist", "Could not download author details of your modlist.", false).ShowDialog());
                return;
            }

            Application.Current.Dispatcher.Invoke(RefreshAuthorData);
        }

        private void OnExploreWorkshop(object? obj)
        {
            if (_searchWindow != null) return;
            _searchWindow = new WorkshopSearch(_api);
            _searchWindow.Closing += OnSearchClosing;
            _searchWindow.ModAdded += OnModAdded;
            _searchWindow.Show();
        }

        private void OnModAdded(object? sender, WorkshopSearchResult mod)
        {
            if (_modlist.Where((x) => x.IsID && x.Mod == mod.PublishedFile.publishedFileID).Any()) return;
            ModFile file = new ModFile(mod.PublishedFile.publishedFileID);
            file.SetManifest(mod.PublishedFile);
            file.AuthorName = mod.AuthorName;
            _modlist.Add(file);
            if (string.IsNullOrEmpty(file.AuthorName))
                LoadModAuthors();
        }

        private void OnSearchClosing(object? sender, CancelEventArgs e)
        {
            if (_searchWindow == null) return;
            _searchWindow.Closing -= OnSearchClosing;
            _searchWindow.ModAdded -= OnModAdded;
            _searchWindow = null;
        }

        private void OnImportFromFile(object? obj)
        {
            throw new NotImplementedException();
        }

        private void OnImportFromText(object? obj)
        {
            throw new NotImplementedException();
        }

        private void OnManifestsLoaded(Task<Dictionary<string, SteamPublishedFile>> task)
        {
            _source?.Dispose();
            _source = null;
            Application.Current.Dispatcher.Invoke(() => OnPropertyChanged("IsLoading"));
            if (!task.IsCompletedSuccessfully)
            {
                if (task.Exception != null)
                    Application.Current.Dispatcher.Invoke(() => new ExceptionModal(task.Exception).ShowDialog());
                else
                    Application.Current.Dispatcher.Invoke(() => new ErrorModal("Modlist", "Could not download mod details of your modlist.", false).ShowDialog());
                return;
            }

            var toAdd = task.Result;
            foreach (var data in toAdd)
                _modManifests[data.Key] = data.Value;

            Application.Current.Dispatcher.Invoke(RefreshModData);
        }

        private void OnModlistChanged()
        {
            _profile.Modlist.Clear();
            foreach (ModFile file in _modlist)
                _profile.Modlist.Add(file.Mod);
            _profile.SaveFile();
        }

        private void OnRefreshManifest(object? obj)
        {
            LoadManifests();
        }

        private void OnSelectionChanged()
        {
            _config.CurrentModlistProfile = _selectedModlist;
            _config.SaveFile();
            LoadModlist();
        }

        private void RefreshAuthorData()
        {
            foreach (ModFile file in _modlist)
                if (file.PublishedFile != null && _api.UsernamesCache.TryGetValue(file.PublishedFile.creator, out var value))
                    file.AuthorName = value;
        }

        private void RefreshModData()
        {
            foreach (ModFile file in _modlist)
                if (file.IsID && _modManifests.TryGetValue(file.Mod, out var value))
                    file.SetManifest(value);
            LoadModAuthors();
        }
    }
}