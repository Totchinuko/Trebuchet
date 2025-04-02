using System;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Media;
using Humanizer;
using SteamKit2.GC.Dota.Internal;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Response;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet.ViewModels
{
    public class WorkshopSearchResult : INotifyPropertyChanged
    {
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
            AddModCommand = new SimpleCommand().Subscribe(() => ModAdded?.Invoke(this, this));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<WorkshopSearchResult>? ModAdded;

        public uint AppId { get; }

        public string Creator { get; private set; } = string.Empty;

        public string CreatorAvatar { get; private set; } = string.Empty;

        public string CreatorId { get; }

        public DateTime LastUpdate { get; }

        public string LastUpdateReadable => $"{LastUpdate.Humanize()}";

        public IImage? PreviewUrl { get; private set; }

        public ulong PublishedFileId { get; }

        public string ShortDescription { get; }

        public long Size { get; }

        public uint Subs { get; }

        public string Title { get; }

        public uint VoteDown { get; }

        public uint VoteUp { get; }
        
        public ICommand AddModCommand { get; }

        public void SetCreator(PlayerSummary summary)
        {
            Creator = summary.PersonaName;
            CreatorAvatar = summary.Avatar;
            OnPropertyChanged(nameof(CreatorAvatar));
            OnPropertyChanged(nameof(Creator));
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async void DownloadCover(string url)
        {
            var cover = await GuiExtensions.DownloadImage(new Uri(url));
            PreviewUrl = cover;
            OnPropertyChanged(nameof(PreviewUrl));
        }
    }
}