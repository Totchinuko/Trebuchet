using Goog;
using GoogLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GoogGUI
{
    /// <summary>
    /// Interaction logic for WorkshopSearch.xaml
    /// </summary>
    public partial class WorkshopSearch : Window, INotifyPropertyChanged
    {
        private SteamWorkWebAPI _api;
        private List<WorkshopSearchResult>? _searchResults = null;
        private string _searchTerm = string.Empty;
        private CancellationTokenSource? _source;

        public WorkshopSearch(SteamWorkWebAPI api)
        {
            _api = api;
            SearchCommand = new SimpleCommand(OnSearch);
            AddModCommand = new SimpleCommand(OnModAdded);
            InitializeComponent();
            DataContext = this;
        }

        public event EventHandler<WorkshopSearchResult>? ModAdded;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand AddModCommand { get; private set; }

        public bool IsSearching => _source != null;

        public ICommand SearchCommand { get; private set; }

        public List<WorkshopSearchResult>? SearchResults { get => _searchResults; set => _searchResults = value; }

        public string SearchTerm { get => _searchTerm; set => _searchTerm = value; }

        protected virtual void OnModAdded(object? obj)
        {
            if (obj is WorkshopSearchResult value)
                ModAdded?.Invoke(this, value);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnSearch(object? obj)
        {
            if (string.IsNullOrEmpty(_searchTerm) || _source != null)
                return;
            _searchResults?.Clear();
            _searchResults = null;
            OnPropertyChanged("SearchResults");

            _source = new CancellationTokenSource();
            OnPropertyChanged("IsSearching");
            Task.Run(() => _api.ExtractWebSearch(_searchTerm, _source.Token)).ContinueWith(OnSearchCompleted);
        }

        private void OnSearchCompleted(Task<List<SteamWebSearchResult>> task)
        {
            _source?.Dispose();
            _source = null;
            Application.Current.Dispatcher.Invoke(() => OnPropertyChanged("IsSearching"));

            if (task.Result == null || task.Result.Count == 0) return;

            _searchResults = new List<WorkshopSearchResult>();
            foreach (var data in task.Result)
                _searchResults.Add(new WorkshopSearchResult(data));
            Application.Current.Dispatcher.Invoke(() => OnPropertyChanged("SearchResults"));
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            OnSearch(sender);
        }
    }
}