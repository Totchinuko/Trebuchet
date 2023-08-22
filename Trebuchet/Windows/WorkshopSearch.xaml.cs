using CommunityToolkit.Mvvm.Messaging;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using SteamWorksWebAPI.Response;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Trebuchet
{
    /// <summary>
    /// Interaction logic for WorkshopSearch.xaml
    /// </summary>
    public partial class WorkshopSearch : Window, INotifyPropertyChanged
    {
        private Config _config;
        private List<WorkshopSearchResult> _searchResults = new List<WorkshopSearchResult>();
        private string _searchTerm = string.Empty;

        public WorkshopSearch(Config config)
        {
            _config = config;
            SearchCommand = new TaskBlockedCommand(OnSearch, true, Operations.SteamSearch);
            AddModCommand = new SimpleCommand(OnModAdded);
            InitializeComponent();
            DataContext = this;
        }

        public event EventHandler<WorkshopSearchResult>? ModAdded;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand AddModCommand { get; private set; }

        public ICommand SearchCommand { get; private set; }

        public List<WorkshopSearchResult> SearchResults
        {
            get => _searchResults;
            set
            {
                _searchResults = value;
                OnPropertyChanged(nameof(SearchResults));
            }
        }

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

        protected override void OnSourceInitialized(EventArgs e)
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;

            if (hwndSource != null && Trebuchet.App.UseSoftwareRendering)
                hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;

            base.OnSourceInitialized(e);
        }

        private void OnCreatorSearchComplete(Task<GetPlayerSummariesResponse> task)
        {
            StrongReferenceMessenger.Default.Send(new OperationReleaseMessage(Operations.SteamSearch));
            if (task.Result.Players.Length == 0) return;

            var enumeration =
                from result in SearchResults
                join player in task.Result.Players on result.CreatorID equals player.SteamID
                select new KeyValuePair<WorkshopSearchResult, PlayerSummary>(result, player);
            foreach (var e in enumeration)
                e.Key.SetCreator(e.Value);
        }

        private void OnSearch(object? obj)
        {
            if (StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.SteamSearch))) return;
            if (string.IsNullOrEmpty(_searchTerm)) return;

            var query = new QueryFilesQuery(App.APIKey)
            {
                Page = 0,
                SearchText = _searchTerm,
                AppId = _config.IsTestLive ? Config.AppIDTestLiveClient : Config.AppIDLiveClient,
                FileType = PublishedFileType.Items_ReadyToUse,
                NumPerPage = 20,
                StripDescriptionBBcode = true,
                ReturnDetails = true,
                QueryType = PublishedFileQueryType.RankedByTextSearch,
                ReturnVoteData = true,
                ReturnShortDescription = true
            };

            CancellationTokenSource cts = StrongReferenceMessenger.Default.Send(new OperationStartMessage(Operations.SteamSearch, 15 * 1000));
            Task.Run(() => PublishedFileService.QueryFiles(query, cts.Token), cts.Token)
                .ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnSearchCompleted(x)))
                .ContinueWith((x) => OnSearchCreators(cts.Token), cts.Token)
                .Unwrap().ContinueWith((x) => Application.Current.Dispatcher.Invoke(() => OnCreatorSearchComplete(x)));
        }

        private void OnSearchCompleted(Task<QueryFilesResponse> task)
        {
            if (task.Result.Total == 0)
                SearchResults = new List<WorkshopSearchResult>();
            else
                SearchResults = task.Result.PublishedFileDetails.Select(file => new WorkshopSearchResult(file)).ToList();
        }

        private async Task<GetPlayerSummariesResponse> OnSearchCreators(CancellationToken ct)
        {
            if (SearchResults.Count == 0) return new GetPlayerSummariesResponse();

            var query = new GetPlayerSummariesQuery(App.APIKey, SearchResults.Select(r => r.CreatorID));
            return await SteamUser.GetPlayerSummaries(query, ct);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            OnSearch(sender);
        }
    }
}