using CommunityToolkit.Mvvm.Messaging;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using SteamWorksWebAPI.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Trebuchet.Messages;
using Trebuchet.ViewModels;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet.Windows
{
    /// <summary>
    /// Interaction logic for WorkshopSearch.xaml
    /// </summary>
    public partial class WorkshopSearch : WindowAutoPadding
    {
        public static readonly DirectProperty<WorkshopSearch, List<WorkshopSearchResult>> SearchResultsProperty =
            AvaloniaProperty.RegisterDirect<WorkshopSearch, List<WorkshopSearchResult>>(nameof(SearchResults),
                o => o.SearchResults, (o, v) => o.SearchResults = v);
        
        private List<WorkshopSearchResult> _searchResults = new List<WorkshopSearchResult>();
        private string _searchTerm = string.Empty;
        private bool _testLiveWorkshop;
        private WorkshopSearchViewModel _workshopSearchViewModel;

        public WorkshopSearch(WorkshopSearchViewModel viewModel)
        {
            _workshopSearchViewModel = viewModel;
            SearchCommand = new TaskBlockedCommand(OnSearch, true, Operations.SteamSearch);
            AddModCommand = new SimpleCommand(OnModAdded);
            InitializeComponent();
            DataContext = this;
        }

        public event EventHandler<WorkshopSearchResult>? ModAdded;

        public ICommand AddModCommand { get; private set; }

        public ICommand SearchCommand { get; private set; }

        public List<WorkshopSearchResult> SearchResults
        {
            get => _searchResults;
            set => SetAndRaise(SearchResultsProperty, ref _searchResults, value);
        }

        public string SearchTerm { get => _searchTerm; set => _searchTerm = value; }

        public bool TestLiveWorkshop
        {
            get => _testLiveWorkshop;
            set
            {
                if (_testLiveWorkshop == value) return;
                _testLiveWorkshop = value;
                OnSearch(this);
            }
        }

        protected virtual void OnModAdded(object? obj)
        {
            if (obj is WorkshopSearchResult value)
                ModAdded?.Invoke(this, value);
        }

        private void OnCreatorSearchComplete(Task<GetPlayerSummariesResponse> task)
        {
            StrongReferenceMessenger.Default.Send(new OperationReleaseMessage(Operations.SteamSearch));
            if (task.Result.Players.Length == 0) return;

            var enumeration =
                from result in SearchResults
                join player in task.Result.Players on result.CreatorId equals player.SteamID
                select new KeyValuePair<WorkshopSearchResult, PlayerSummary>(result, player);
            foreach (var e in enumeration)
                e.Key.SetCreator(e.Value);
        }

        private void OnSearch(object? obj)
        {
            if (StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.SteamSearch))) return;
            if (string.IsNullOrEmpty(_searchTerm)) return;

            var query = new QueryFilesQuery()
            {
                Page = 0,
                SearchText = _searchTerm,
                AppId = TestLiveWorkshop ? Config.AppIDTestLiveClient : Config.AppIDLiveClient,
                FileType = PublishedFileType.Items_ReadyToUse,
                NumPerPage = 20,
                StripDescriptionBBcode = true,
                ReturnDetails = true,
                QueryType = PublishedFileQueryType.RankedByTextSearch,
                ReturnVoteData = true,
                ReturnShortDescription = true
            };

            CancellationTokenSource cts
                = StrongReferenceMessenger.Default.Send(new OperationStartMessage(Operations.SteamSearch, 15 * 1000));
            Task.Run(() => PublishedFileService.QueryFiles(query, cts.Token), cts.Token)
                .ContinueWith((x) => Dispatcher.UIThread.Invoke(() => OnSearchCompleted(x)), cts.Token)
                .ContinueWith((_) => OnSearchCreators(cts.Token), cts.Token)
                .Unwrap().ContinueWith((x) => Dispatcher.UIThread.Invoke(() => OnCreatorSearchComplete(x)), cts.Token);
        }

        private void OnSearchCompleted(Task<QueryFilesResponse> task)
        {
            SearchResults = task.Result.Total == 0
                ? []
                : task.Result.PublishedFileDetails.Select(file => new WorkshopSearchResult(file)).ToList();
        }

        private async Task<GetPlayerSummariesResponse> OnSearchCreators(CancellationToken ct)
        {
            if (SearchResults.Count == 0) return new GetPlayerSummariesResponse();

            var query = new GetPlayerSummariesQuery(App.ApiKey, SearchResults.Select(r => r.CreatorId));
            return await SteamUser.GetPlayerSummaries(query, ct);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            OnSearch(sender);
        }
    }
}