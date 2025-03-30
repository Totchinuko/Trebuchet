using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using SteamWorksWebAPI.Response;
using Trebuchet.Panels;
using TrebuchetLib;
using TrebuchetLib.Services;
using TrebuchetUtils;

namespace Trebuchet.ViewModels;

public class WorkshopSearchViewModel : INotifyPropertyChanged
{
    private List<WorkshopSearchResult> _searchResults = [];
    private bool _testLiveWorkshop;
    private string _searchTerm = string.Empty;
    private readonly AppSettings _appSettings;

    public WorkshopSearchViewModel(AppSettings appSettings)
    {
        _appSettings = appSettings;
        SearchCommand = new SimpleCommand((_) => OnSearch());
        AddModCommand = new SimpleCommand(OnModAdded);
    }

    public event EventHandler<WorkshopSearchResult>? ModAdded;

    public List<WorkshopSearchResult> SearchResults
    {
        get => _searchResults;
        set => SetField(ref _searchResults, value);
    }
    
    public ICommand AddModCommand { get; }

    public ICommand SearchCommand { get; }

    public bool TestLiveWorkshop
    {
        get => _testLiveWorkshop;
        set
        {
            if(SetField(ref _testLiveWorkshop, value))
                Search(_searchTerm, _testLiveWorkshop);
        }
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
        SearchResults = files.Total == 0
            ? []
            : files.PublishedFileDetails.Select(file => new WorkshopSearchResult(file)).ToList();

        var summary = new GetPlayerSummariesResponse();
        if (files.Total != 0)
        {
            var playerQuery = new GetPlayerSummariesQuery(_appSettings.ApiKey, SearchResults.Select(r => r.CreatorId));
            summary = await SteamUser.GetPlayerSummaries(playerQuery, CancellationToken.None);
        } 
        if (summary.Players.Length == 0) return;

        var enumeration =
            from result in SearchResults
            join player in summary.Players on result.CreatorId equals player.SteamID
            select new KeyValuePair<WorkshopSearchResult, PlayerSummary>(result, player);
        foreach (var e in enumeration)
            e.Key.SetCreator(e.Value);
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnModAdded(object? obj)
    {
        if (obj is WorkshopSearchResult value)
            ModAdded?.Invoke(this, value);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}