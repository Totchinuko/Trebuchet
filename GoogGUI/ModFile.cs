using Goog;
using SteamWorksWebAPI;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace GoogGUI
{
    internal class ModFile : INotifyPropertyChanged
    {
        private PublishedFile? _file;
        private FileInfo _infos;
        private DateTime _lastUpdate;
        private ulong _publishedFileID = 0;

        public ModFile(string path)
        {
            _infos = new FileInfo(path);
        }

        public ModFile(ulong publishedFileID, string path)
        {
            _publishedFileID = publishedFileID;
            _infos = new FileInfo(path);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsPublished => _publishedFileID > 0;

        public string LastUpdate
        {
            get
            {
                if (!IsPublished && !_infos.Exists) return string.Empty;
                if(!IsPublished)
                {
                    DateTime lastModified = _infos.LastWriteTime;
                    return "Last Modified : " + lastModified.ToShortDateString() + " " + lastModified.ToShortTimeString();
                }

                if (_file == null) return "Loading...";
                DateTime local = _lastUpdate.ToLocalTime();
                return "Last Update : " + local.ToShortDateString() + " " + local.ToShortTimeString();
            }
        }

        public ulong PublishedFileID => _publishedFileID;

        public string Title
        {
            get
            {
                if (_file != null)
                    return _file.Title;
                if (IsPublished)
                    return _publishedFileID.ToString();
                return _infos.Name;
            }
        }

        public PublishedFile? PublishedFile => _file;

        public Brush StatusColor => GetStatusBrush();

        public string StatusTooltip => GetStatusText();

        public void SetManifest(PublishedFile file)
        {
            if (!IsPublished)
                throw new Exception("Cannot set a manifest on a local mod.");
            _file = file;
            _lastUpdate = Tools.UnixTimeStampToDateTime(_file.TimeUpdated);
            OnPropertyChanged("ModName");
            OnPropertyChanged("LastUpdate");
            OnPropertyChanged("StatusColor");
            OnPropertyChanged("StatusTooltip");
        }

        public void RefreshFile(string path)
        {
            _infos = new FileInfo(path);
            OnPropertyChanged("StatusColor");
            OnPropertyChanged("StatusTooltip");
        }

        protected virtual Brush GetStatusBrush()
        {
            if (!_infos.Exists) return (Brush)Application.Current.Resources["GDimRed"];
            if (_file == null) return (Brush)Application.Current.Resources["GDimBlue"];
            if (_lastUpdate < _infos.LastWriteTimeUtc && _file.FileSize == _infos.Length) return (Brush)Application.Current.Resources["GDimGreen"];
            if (_lastUpdate < _infos.LastWriteTimeUtc && _file.FileSize != _infos.Length) return (Brush)Application.Current.Resources["GDimYellow"];
            return (Brush)Application.Current.Resources["GDimYellow"];
        }

        protected virtual string GetStatusText()
        {
            if (!_infos.Exists) return "Missing";
            if (_file == null) return "Found";
            if (_lastUpdate < _infos.LastWriteTimeUtc && _file.FileSize == _infos.Length) return "Up to Date";
            if (_lastUpdate < _infos.LastWriteTimeUtc && _file.FileSize != _infos.Length) return "Corrupted";
            return "Update available";
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}