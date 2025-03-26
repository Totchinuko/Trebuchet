using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using SteamWorksWebAPI;
using Trebuchet.Messages;
using Trebuchet.Services;
using TrebuchetLib;

namespace Trebuchet.ViewModels;

public class WorkshopSearchViewModel(AppSettings appSettings, Config config)
{
    private AppSettings _appSettings = appSettings;
    private Config _config = config;

    public void Search(string searchTerm, bool testLive)
    {
        if (StrongReferenceMessenger.Default.Send(new OperationStateRequest(Operations.SteamSearch))) return;
        if (string.IsNullOrEmpty(searchTerm)) return;

        var query = new QueryFilesQuery(_appSettings.ApiKey)
        {
            Page = 0,
            SearchText = searchTerm,
            AppId = testLive ? config.AppIDTestLiveClient : config.AppIDLiveClient,
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
}