using System;
using System.Reactive;
using Avalonia.Media;
using Humanizer;
using ReactiveUI;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Response;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet.ViewModels
{
    public class WorkshopSearchResult : BaseViewModel
    {
        private string _creatorAvatar = string.Empty;
        private string _creator = string.Empty;
        private IImage? _previewUrl;

        public WorkshopSearchResult(QueriedPublishedFile result)
        {
            AppId = result.ConsumerAppID;
            CreatorId = result.Creator;
            LastUpdate = Tools.UnixTimeStampToDateTime(result.TimeUpdated);
            PublishedFileId = result.PublishedFileID;
            ShortDescription = result.ShortDescription;
            Size = result.FileSize;
            Subs = result.Subscriptions;
            Title = result.Title;
            VoteDown = result.VoteData.VotesDown;
            VoteUp = result.VoteData.VotesUp;
            DownloadCover(result.PreviewUrl);
            AddModCommand = ReactiveCommand.Create(() => ModAdded?.Invoke(this, this));
            OpenWeb = ReactiveCommand.Create(() =>
            {
                TrebuchetUtils.Utils.OpenWeb(string.Format(Constants.SteamWorkshopURL, PublishedFileId));
            });
        }

        public event EventHandler<WorkshopSearchResult>? ModAdded;

        public uint AppId { get; }

        public string Creator
        {
            get => _creator;
            private set => SetField(ref _creator, value);
        }

        public string CreatorAvatar
        {
            get => _creatorAvatar;
            private set => SetField(ref _creatorAvatar, value);
        }

        public string CreatorId { get; }

        public DateTime LastUpdate { get; }

        public string LastUpdateReadable => LastUpdate.Humanize();

        public IImage? PreviewUrl
        {
            get => _previewUrl;
            private set => SetField(ref _previewUrl, value);
        }

        public ulong PublishedFileId { get; }

        public string ShortDescription { get; }

        public long Size { get; }

        public uint Subs { get; }

        public string Title { get; }

        public uint VoteDown { get; }

        public uint VoteUp { get; }
        
        public ReactiveCommand<Unit,Unit> AddModCommand { get; }
        public ReactiveCommand<Unit,Unit> OpenWeb { get; }

        public void SetCreator(PlayerSummary summary)
        {
            Creator = summary.PersonaName;
            CreatorAvatar = summary.Avatar;
        }

        private async void DownloadCover(string url)
        {
            var cover = await GuiExtensions.DownloadImage(new Uri(url));
            PreviewUrl = cover;
        }
    }
}