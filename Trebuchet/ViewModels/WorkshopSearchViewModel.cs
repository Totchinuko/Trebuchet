using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Windows.Input;
using ReactiveUI;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using SteamWorksWebAPI.Response;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;

namespace Trebuchet.ViewModels;

public class WorkshopSearchViewModel : BaseViewModel
{
    private ObservableCollection<WorkshopSearchResult> _searchResults = [];
    private bool _testLiveWorkshop;
    private string _searchTerm = string.Empty;
    private readonly AppSettings _appSettings;
    private bool _isLoading;

    public WorkshopSearchViewModel(AppSettings appSettings)
    {
        _appSettings = appSettings;
        SearchCommand = ReactiveCommand.Create(OnSearch);
    }

    public event EventHandler<WorkshopSearchResult>? ModAdded;

    public ObservableCollection<WorkshopSearchResult> SearchResults
    {
        get => _searchResults;
        set => SetField(ref _searchResults, value);
    }
    
    public ReactiveCommand<Unit,Unit> SearchCommand { get; }

    public bool TestLiveWorkshop
    {
        get => _testLiveWorkshop;
        set
        {
            if(SetField(ref _testLiveWorkshop, value))
                Search(_searchTerm, _testLiveWorkshop);
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    public string SearchTerm
    {
        get => _searchTerm;
        set => SetField(ref _searchTerm, value);
    }

    public void OnSearch()
    {
        Search(_searchTerm, _testLiveWorkshop);
    }

    public async void Search(string searchTerm, bool testLive)
    {
        IsLoading = true;
        var query = new QueryFilesQuery(_appSettings.ApiKey)
        {
            Page = 0,
            SearchText = searchTerm,
            AppId = testLive ? Constants.AppIDTestLiveClient : Constants.AppIDLiveClient,
            FileType = PublishedFileType.Items_ReadyToUse,
            NumPerPage = 20,
            StripDescriptionBBcode = true,
            ReturnDetails = true,
            QueryType = PublishedFileQueryType.RankedByTextSearch,
            ReturnVoteData = true,
            ReturnShortDescription = true
        };

        var files = await PublishedFileService.QueryFiles(query, CancellationToken.None);
        if (files.Total > 0)
        {
            SearchResults.Clear();
            foreach (var file in files.PublishedFileDetails)
            {
                var searchResult = new WorkshopSearchResult(file);
                searchResult.ModAdded += OnModAdded;
                SearchResults.Add(searchResult);
            }
        }
        else
            SearchResults.Clear();

        var summary = new GetPlayerSummariesResponse();
        if (files.Total != 0)
        {
            var playerQuery = new GetPlayerSummariesQuery(_appSettings.ApiKey, SearchResults.Select(r => r.CreatorId));
            summary = await SteamUser.GetPlayerSummaries(playerQuery, CancellationToken.None);
        }

        if (summary.Players.Length == 0)
        {
            IsLoading = false;
            return;
        }

        var enumeration =
            from result in SearchResults
            join player in summary.Players on result.CreatorId equals player.SteamID
            select new KeyValuePair<WorkshopSearchResult, PlayerSummary>(result, player);
        foreach (var e in enumeration)
            e.Key.SetCreator(e.Value);
        IsLoading = false;
    }
    
    protected virtual void OnModAdded(object? sender, WorkshopSearchResult result)
    {
        ModAdded?.Invoke(this, result);
    }
}