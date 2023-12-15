using SteamWorksWebAPI;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Trebuchet
{
    public class ModFile : INotifyPropertyChanged
    {
        private FileInfo _infos;
        private DateTime _lastUpdate;
        private ulong _publishedFileID = 0;
        private long _size = 0;
        private string _title = string.Empty;

        public ModFile(string path)
        {
            _infos = new FileInfo(path);
        }

        public ModFile(ulong publishedFileID, string path)
        {
            _publishedFileID = publishedFileID;
            _infos = new FileInfo(path);
        }

        public ModFile(WorkshopSearchResult search, string path)
        {
            _title = search.Title;
            _publishedFileID = search.PublishedFileID;
            _size = search.Size;
            _lastUpdate = search.LastUpdate;
            _infos = new FileInfo(path);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsPublished => _publishedFileID > 0;

        public string LastUpdate
        {
            get
            {
                if (!IsPublished && !_infos.Exists) return string.Empty;
                if (!IsPublished)
                {
                    DateTime lastModified = _infos.LastWriteTime;
                    return "Last Modified : " + lastModified.ToShortDateString() + " " + lastModified.ToShortTimeString();
                }

                if (_lastUpdate == default) return "Loading...";
                DateTime local = _lastUpdate.ToLocalTime();
                return "Last Update : " + local.ToShortDateString() + " " + local.ToShortTimeString();
            }
        }

        public ulong PublishedFileID => _publishedFileID;

        public Brush StatusColor => GetStatusBrush();

        public string StatusTooltip => GetStatusText();

        public string Title
        {
            get
            {
                if (!string.IsNullOrEmpty(_title))
                    return _title;
                if (IsPublished)
                    return _publishedFileID.ToString();
                return _infos.Name;
            }
        }

        public void RefreshFile(string path)
        {
            _infos = new FileInfo(path);
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusTooltip));
        }

        public void SetManifest(PublishedFile file)
        {
            if (!IsPublished)
                throw new Exception("Cannot set a manifest on a local mod.");
            _lastUpdate = Tools.UnixTimeStampToDateTime(file.TimeUpdated);
            _title = file.Title;
            _size = file.FileSize;
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(LastUpdate));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusTooltip));
        }

        public override string ToString()
        {
            return IsPublished ? PublishedFileID.ToString() : _infos.FullName;
        }

        protected virtual Brush GetStatusBrush()
        {
            if (!_infos.Exists) return (Brush)Application.Current.Resources["GDimRed"];
            if (_publishedFileID == 0) return (Brush)Application.Current.Resources["GDimBlue"];
            if (_lastUpdate < _infos.LastWriteTimeUtc) return (Brush)Application.Current.Resources["GDimGreen"];
            //if (_lastUpdate < _infos.LastWriteTimeUtc && _size != _infos.Length) return (Brush)Application.Current.Resources["GDimYellow"];
            return (Brush)Application.Current.Resources["GDimYellow"];
        }

        protected virtual string GetStatusText()
        {
            if (!_infos.Exists) return "Missing";
            if (_publishedFileID == 0) return "Found";
            if (_lastUpdate < _infos.LastWriteTimeUtc) return "Up to Date";
            //if (_lastUpdate < _infos.LastWriteTimeUtc && _size != _infos.Length) return "Corrupted";
            return "Update available";
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}