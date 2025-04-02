﻿using System;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Humanizer;
using SteamWorksWebAPI;
using Trebuchet.Assets;
using Trebuchet.Services.TaskBlocker;
using TrebuchetLib;
using TrebuchetUtils;

namespace Trebuchet.ViewModels
{
    public class ModFile : BaseViewModel
    {
        private uint _appId;
        private FileInfo _infos;
        private DateTime _lastUpdate;
        private bool _needUpdate;
        private long _size;
        private string _title = string.Empty;

        public ModFile(string path)
        {
            _infos = new FileInfo(path);
            RemoveModCommand = new SimpleCommand();
            OpenModPageCommand = new SimpleCommand();
            UpdateModCommand = new TaskBlockedCommand()
                .SetBlockingType<SteamDownload>();
        }

        public ModFile(ulong publishedFileId, string path) : this(path)
        {
            PublishedFileId = publishedFileId;
        }

        public ModFile(WorkshopSearchResult search, string path) : this(path)
        {
            _title = search.Title;
            PublishedFileId = search.PublishedFileId;
            _appId = search.AppId;
            _size = search.Size;
            _lastUpdate = search.LastUpdate;
        }

        public SimpleCommand RemoveModCommand { get; }
        public SimpleCommand OpenModPageCommand { get; }
        public SimpleCommand UpdateModCommand { get; }
        
        public IBrush BorderColor => GetBorderBrush();
        public bool IsPublished => PublishedFileId > 0;
        public bool IsTestLive => _appId == Constants.AppIDTestLiveClient;
        public string TypeTooltip => GetTypeText();
        public string ModType => IsTestLive ? "TestLive" : "Live";
        public ulong PublishedFileId { get; }
        public IBrush StatusColor => GetStatusBrush();
        public string StatusTooltip => GetStatusText();
        public string LastUpdate
        {
            get
            {
                if (!IsPublished && !_infos.Exists) return string.Empty;
                if (!IsPublished)
                {
                    DateTime lastModified = _infos.LastWriteTime;
                    return $"{Resources.LastModified}: {_infos.LastWriteTime.Humanize()}";
                }

                if (_lastUpdate == default) return $"{Resources.Loading}...";
                DateTime local = _lastUpdate.ToLocalTime();
                return $"{Resources.LastUpdate}: {local.Humanize()}";
            }
        }
        public string Title
        {
            get
            {
                if (!string.IsNullOrEmpty(_title))
                    return _title;
                if (IsPublished)
                    return PublishedFileId.ToString();
                return _infos.Name;
            }
        }

        public void RefreshFile(string path)
        {
            _infos = new FileInfo(path);
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(StatusTooltip));
        }

        public void SetManifest(PublishedFile file, bool needUpdate = false)
        {
            if (!IsPublished)
                throw new Exception("Cannot set a manifest on a local mod.");
            _lastUpdate = Tools.UnixTimeStampToDateTime(file.TimeUpdated);
            _title = file.Title;
            _size = file.FileSize;
            _appId = file.ConsumerAppId;
            _needUpdate = needUpdate;
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(LastUpdate));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(StatusTooltip));
        }

        public override string ToString()
        {
            return IsPublished ? PublishedFileId.ToString() : _infos.FullName;
        }

        protected virtual IBrush GetBorderBrush()
        {
            if (!_infos.Exists) return GetBrush("TRed");
            if (PublishedFileId == 0) return GetBrush("TBlue");
            if (!_needUpdate) return GetBrush("TGreen");
            return GetBrush("TYellow");
        }

        protected virtual IBrush GetStatusBrush()
        {
            if (!_infos.Exists) return GetBrush("TRedDim");
            if (PublishedFileId == 0) return GetBrush("TBlueDim");
            if (!_needUpdate) return GetBrush("TGreenDim");
            return GetBrush("TYellowDim");
        }

        private IBrush GetBrush(string name)
        {
            if(Application.Current == null) throw new Exception("Application.Current is null");

            if (Application.Current.Styles.TryGetResource(name, Application.Current.ActualThemeVariant,
                    out var resource) && resource is IBrush brush)
            {
                return brush;
            }
            throw new Exception("Resource not found: " + name);
        }

        private string GetTypeText()
        {
            if(IsPublished)
                return IsTestLive ? Resources.TestLiveMod : Resources.LiveMod;
            return Resources.LocalMod;
        }

        protected virtual string GetStatusText()
        {
            if (!_infos.Exists) return Resources.Missing;
            if (PublishedFileId == 0) return Resources.Found;
            if (!_needUpdate) return Resources.UpToDate;
            //if (_lastUpdate < _infos.LastWriteTimeUtc) return "Up to Date";
            //if (_lastUpdate < _infos.LastWriteTimeUtc && _size != _infos.Length) return "Corrupted";
            return Resources.UpdateAvailable;
        }
    }
}