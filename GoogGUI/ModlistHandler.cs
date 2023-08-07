﻿using Goog;
using GoogLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
        private Dictionary<string, SteamPublishedFile> _modManigests = new Dictionary<string, SteamPublishedFile>();
        private ModListProfile _profile = new ModListProfile();
        private string _selectedModlist = string.Empty;
        private CancellationTokenSource? _source;

        public ModlistHandler(Config config)
        {
            RefreshManifestCommand = new SimpleCommand(OnRefreshManifest);
            ImportFromFileCommand = new SimpleCommand(OnImportFromFile);
            ImportFromTextCommand = new SimpleCommand(OnImportFromText);
            ImportFromURLCommand = new SimpleCommand(OnExploreWorkshop);
            ExploreWorkshopCommand = new SimpleCommand(OnExploreWorkshop);

            _modlist = new TrulyObservableCollection<ModFile>
            {
                new ModFile("2886779102"),
                new ModFile("2850232250"),
                new ModFile("2847709656"),
                new ModFile("2684530805"),
                new ModFile("2677532697"),
            };

            _config = config;
            _api = new SteamWorkWebAPI(_config.SteamAPIKey);

            _selectedModlist = _config.CurrentModlistProfile;
            LoadManifests();
            //LoadModlist();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand ExploreWorkshopCommand { get; private set; }

        public ICommand ImportFromFileCommand { get; private set; }

        public ICommand ImportFromTextCommand { get; private set; }

        public ICommand ImportFromURLCommand { get; private set; }

        public bool IsLoading => _source != null;

        public ICommand RefreshManifestCommand { get; private set; }

        public object Template => Application.Current.Resources["ModlistEditor"];

        public object ItemTemplate => Application.Current.Resources["ModlistItems"];

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                OnSelectionChanged();
            }
        }

        public TrulyObservableCollection<ModFile> Modlist
        {
            get => _modlist;
            set
            {
                _modlist = value;
                OnModlistChanged();
            }
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void LoadManifests()
        {
            if (_source != null) return;

            _source = new CancellationTokenSource();
            OnPropertyChanged("IsLoading");
            List<string> resquested = new List<string>();
            foreach (ModFile file in _modlist)
                if (file.IsID && !_modManigests.ContainsKey(file.Mod))
                    resquested.Add(file.Mod);

            Task.Run(() => _api.GetPublishedFiles(resquested, _source.Token)).ContinueWith(OnManifestsLoaded);
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

        private void OnExploreWorkshop(object? obj)
        {
            throw new NotImplementedException();
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
                _modManigests[data.Key] = data.Value;

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

        private void RefreshModData()
        {
            foreach (ModFile file in _modlist)
                if (file.IsID && _modManigests.TryGetValue(file.Mod, out var value))
                    file.SetManifest(value);
        }
    }
}