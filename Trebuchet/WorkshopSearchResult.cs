using System;
using System.ComponentModel;
using Humanizer;
using SteamWorksWebAPI;
using SteamWorksWebAPI.Response;
using TrebuchetLib;

namespace Trebuchet
{
    public class WorkshopSearchResult : INotifyPropertyChanged
    {
        private uint _appID;
        private string _creator = string.Empty;
        private string _creatorAvatar = string.Empty;
        private string _creatorID;
        private DateTime _lastUpdate;
        private string _previewURL;
        private ulong _publishedFileID;
        private string _shortDescription;
        private long _size;
        private uint _subs;
        private string _title;
        private uint _voteDown;
        private uint _voteUp;

        public WorkshopSearchResult(QueriedPublishedFile result)
        {
            _previewURL = result.PreviewUrl;
            _publishedFileID = result.PublishedFileID;
            _title = result.Title;
            _voteDown = result.VoteData.VotesDown;
            _voteUp = result.VoteData.VotesUp;
            _size = result.FileSize;
            _lastUpdate = Tools.UnixTimeStampToDateTime(result.TimeUpdated);
            _creatorID = result.Creator;
            _shortDescription = result.ShortDescription;
            _subs = result.Subscriptions;
            _appID = result.ConsumerAppID;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public uint AppID => _appID;

        public string Creator => _creator;

        public string CreatorAvatar => _creatorAvatar;

        public string CreatorID => _creatorID;

        public DateTime LastUpdate => _lastUpdate;

        public string LastUpdateReadable => $"{_lastUpdate.Humanize()}";

        public string PreviewURL => _previewURL;

        public ulong PublishedFileID => _publishedFileID;

        public string ShortDescription => _shortDescription;

        public long Size => _size;

        public uint Subs => _subs;

        public string Title => _title;

        public uint VoteDown => _voteDown;

        public uint VoteUp => _voteUp;

        public void SetCreator(PlayerSummary summary)
        {
            _creator = summary.PersonaName;
            _creatorAvatar = summary.Avatar;
            OnPropertyChanged(nameof(CreatorAvatar));
            OnPropertyChanged(nameof(Creator));
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}