using System;
using System.Reactive;
using Humanizer;
using ReactiveUI;
using SteamKit2.Internal;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet.ViewModels
{
    public class WorkshopSearchResult : BaseViewModel
    {
        public WorkshopSearchResult(PublishedFileDetails result)
        {
            AppId = result.consumer_appid;
            CreatorId = result.creator;
            LastUpdate = Tools.UnixTimeStampToDateTime(result.time_updated);
            PublishedFileId = result.publishedfileid;
            ShortDescription = result.short_description;
            Size = result.file_size;
            Subs = result.subscriptions;
            Title = result.title;
            VoteDown = result.vote_data.votes_down;
            VoteUp = result.vote_data.votes_up;
            PreviewUrl = result.preview_url;
            AddModCommand = ReactiveCommand.Create(() => ModAdded?.Invoke(this, this));
            OpenWeb = ReactiveCommand.Create(() =>
            {
                tot_lib.Utils.OpenWeb(string.Format(Constants.SteamWorkshopURL, PublishedFileId));
            });
        }

        public event EventHandler<WorkshopSearchResult>? ModAdded;

        public uint AppId { get; }

        public ulong CreatorId { get; }

        public DateTime LastUpdate { get; }

        public string LastUpdateReadable => LastUpdate.Humanize();

        public ulong PublishedFileId { get; }

        public string ShortDescription { get; }
        
        public string PreviewUrl { get; }

        public ulong Size { get; }

        public uint Subs { get; }

        public string Title { get; }

        public uint VoteDown { get; }

        public uint VoteUp { get; }
        
        public ReactiveCommand<Unit,Unit> AddModCommand { get; }
        public ReactiveCommand<Unit,Unit> OpenWeb { get; }
    }
}