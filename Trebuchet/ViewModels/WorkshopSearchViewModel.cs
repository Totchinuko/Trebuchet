using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Input;
using DynamicData.Binding;
using ReactiveUI;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using SteamWorksWebAPI.Response;
using TrebuchetLib;
using TrebuchetLib.Services;

namespace Trebuchet.ViewModels;

public class WorkshopSearchViewModel : ReactiveObject
{


    public WorkshopSearchViewModel(Steam steam)
    {
        _steam = steam;
        SearchCommand = ReactiveCommand.Create(() =>
        {
            Page = 1;
            Search(_searchTerm, _testLiveWorkshop, 1);
        });

        NextPage = ReactiveCommand.Create<Unit>((_) =>
        {
            Page++;
        }, this.WhenAnyValue(x => x.Page, x => x.MaxPage, (p, m) => p < m));
        
        PreviousPage = ReactiveCommand.Create<Unit>((_) =>
        {
            Page--;
        }, this.WhenAnyValue(x => x.Page, (p) => p > 1));

        this.WhenValueChanged<WorkshopSearchViewModel, bool>(x => x.TestLiveWorkshop, false, () => false)
            .Select(_ => Unit.Default)
            .InvokeCommand(SearchCommand);
        this.WhenValueChanged<WorkshopSearchViewModel, uint>(x => x.Page, false, () => 1)
            .Subscribe((p) => Search(SearchTerm, TestLiveWorkshop, p));
    }
    
    private ObservableCollection<WorkshopSearchResult> _searchResults = [];
    private bool _testLiveWorkshop;
    private string _searchTerm = string.Empty;
    private readonly Steam _steam;
    private bool _isLoading;
    private int _maxPage = 1;
    private uint _page = 1;


    public event EventHandler<WorkshopSearchResult>? ModAdded;
    public event EventHandler? PageLoaded;
    public ObservableCollectionExtended<WorkshopSearchResult> SearchResults { get; } = [];
    
    public ReactiveCommand<Unit,Unit> SearchCommand { get; }
    public ReactiveCommand<Unit,Unit> NextPage { get; }
    public ReactiveCommand<Unit,Unit> PreviousPage { get; }

    public bool TestLiveWorkshop
    {
        get => _testLiveWorkshop;
        set => this.RaiseAndSetIfChanged(ref _testLiveWorkshop, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public int MaxPage
    {
        get => _maxPage;
        set => this.RaiseAndSetIfChanged(ref _maxPage, value);
    }

    public uint Page
    {
        get => _page;
        set => this.RaiseAndSetIfChanged(ref _page, value);
    }

    public string SearchTerm
    {
        get => _searchTerm;
        set => this.RaiseAndSetIfChanged(ref _searchTerm, value);
    }

    public async void Search(string searchTerm, bool testLive, uint page)
    {
        IsLoading = true;

        var appId = testLive ? Constants.AppIDTestLiveClient : Constants.AppIDLiveClient;
        var wresult = await _steam.QueryWorkshopSearch(appId, searchTerm, 20, page);
        if (wresult is null)
        {
            IsLoading = false;
            return;
        }
        MaxPage = Math.Max((int)wresult.total / 20, 1);
        if (wresult.total > 0)
        {
            SearchResults.Clear();
            foreach (var file in wresult.publishedfiledetails)
            {
                var searchResult = new WorkshopSearchResult(file);
                searchResult.ModAdded += OnModAdded;
                SearchResults.Add(searchResult);
            }
        }
        else
            SearchResults.Clear();

        PageLoaded?.Invoke(this, EventArgs.Empty);
        IsLoading = false;
    }
    
    protected virtual void OnModAdded(object? sender, WorkshopSearchResult result)
    {
        ModAdded?.Invoke(this, result);
    }
}